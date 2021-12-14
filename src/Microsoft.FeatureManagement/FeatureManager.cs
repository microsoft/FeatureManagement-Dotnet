// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to evaluate whether a feature is enabled or disabled.
    /// </summary>
    class FeatureManager : IFeatureManager, IDynamicFeatureManager
    {
        private readonly IFeatureDefinitionProvider _featureDefinitionProvider;
        private readonly IEnumerable<IFeatureFilterMetadata> _featureFilters;
        private readonly IEnumerable<IFeatureVariantAssignerMetadata> _variantAssigners;
        private readonly IFeatureVariantOptionsResolver _variantOptionsResolver;
        private readonly IEnumerable<ISessionManager> _sessionManagers;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, IFeatureFilterMetadata> _filterMetadataCache;
        private readonly ConcurrentDictionary<string, IFeatureVariantAssignerMetadata> _assignerMetadataCache;
        private readonly ConcurrentDictionary<string, ContextualFeatureFilterEvaluator> _contextualFeatureFilterCache;
        private readonly ConcurrentDictionary<string, ContextualFeatureVariantAssignerEvaluator> _contextualFeatureVariantAssignerCache;
        private readonly FeatureManagementOptions _options;

        public FeatureManager(
            IFeatureDefinitionProvider featureDefinitionProvider,
            IEnumerable<IFeatureFilterMetadata> featureFilters,
            IEnumerable<IFeatureVariantAssignerMetadata> variantAssigner,
            IFeatureVariantOptionsResolver variantOptionsResolver,
            IEnumerable<ISessionManager> sessionManagers,
            ILoggerFactory loggerFactory,
            IOptions<FeatureManagementOptions> options)
        {
            _featureFilters = featureFilters ?? throw new ArgumentNullException(nameof(featureFilters));
            _variantAssigners = variantAssigner ?? throw new ArgumentNullException(nameof(variantAssigner));
            _variantOptionsResolver = variantOptionsResolver ?? throw new ArgumentNullException(nameof(variantOptionsResolver));
            _featureDefinitionProvider = featureDefinitionProvider ?? throw new ArgumentNullException(nameof(featureDefinitionProvider));
            _sessionManagers = sessionManagers ?? throw new ArgumentNullException(nameof(sessionManagers));
            _logger = loggerFactory.CreateLogger<FeatureManager>();
            _filterMetadataCache = new ConcurrentDictionary<string, IFeatureFilterMetadata>(StringComparer.OrdinalIgnoreCase);
            _contextualFeatureFilterCache = new ConcurrentDictionary<string, ContextualFeatureFilterEvaluator>(StringComparer.OrdinalIgnoreCase);
            _assignerMetadataCache = new ConcurrentDictionary<string, IFeatureVariantAssignerMetadata>(StringComparer.OrdinalIgnoreCase);
            _contextualFeatureVariantAssignerCache = new ConcurrentDictionary<string, ContextualFeatureVariantAssignerEvaluator>(StringComparer.OrdinalIgnoreCase);
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Task<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken)
        {
            return IsEnabledAsync<object>(feature, null, false, cancellationToken);
        }

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext, CancellationToken cancellationToken)
        {
            return IsEnabledAsync(feature, appContext, true, cancellationToken);
        }

        public async IAsyncEnumerable<string> GetFeatureFlagNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (FeatureFlagDefinition featureDefintion in _featureDefinitionProvider.GetAllFeatureFlagDefinitionsAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return featureDefintion.Name;
            }
        }

        public async IAsyncEnumerable<string> GetDynamicFeatureNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (DynamicFeatureDefinition featureDefintion in _featureDefinitionProvider.GetAllDynamicFeatureDefinitionsAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return featureDefintion.Name;
            }
        }

        public ValueTask<T> GetVariantAsync<T, TContext>(string feature, TContext appContext, CancellationToken cancellationToken)
        {
            return GetVariantAsync<T, TContext>(feature, appContext, true, cancellationToken);
        }

        public ValueTask<T> GetVariantAsync<T>(string feature, CancellationToken cancellationToken)
        {
            return GetVariantAsync<T, object>(feature, null, false, cancellationToken);
        }

        private async ValueTask<T> GetVariantAsync<T, TContext>(string feature, TContext appContext, bool useAppContext, CancellationToken cancellationToken)
        {
            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            FeatureVariant variant = null;

            DynamicFeatureDefinition featureDefinition = await _featureDefinitionProvider
                .GetDynamicFeatureDefinitionAsync(feature, cancellationToken)
                .ConfigureAwait(false);

            if (featureDefinition == null)
            {
                throw new FeatureManagementException(
                    FeatureManagementError.MissingFeature,
                    $"The feature declaration for the dynamic feature '{feature}' was not found.");
            }

            if (string.IsNullOrEmpty(featureDefinition.Assigner))
            {
                throw new FeatureManagementException(
                    FeatureManagementError.MissingFeatureVariantAssigner,
                    $"Missing feature variant assigner name for the feature {feature}");
            }

            if (featureDefinition.Variants == null)
            {
                throw new FeatureManagementException(
                    FeatureManagementError.MissingFeatureVariant,
                    $"No variants are registered for the feature {feature}");
            }

            FeatureVariant defaultVariant = null;

            foreach (FeatureVariant v in featureDefinition.Variants)
            {
                if (v.Default)
                {
                    if (defaultVariant != null)
                    {
                        throw new FeatureManagementException(
                            FeatureManagementError.AmbiguousDefaultFeatureVariant,
                            $"Multiple default variants are registered for the feature '{feature}'.");
                    }

                    defaultVariant = v;
                }

                if (v.ConfigurationReference == null)
                {
                    throw new FeatureManagementException(
                        FeatureManagementError.MissingConfigurationReference,
                        $"The variant '{variant.Name}' for the feature '{feature}' does not have a configuration reference.");
                }
            }

            if (defaultVariant == null)
            {
                throw new FeatureManagementException(
                    FeatureManagementError.MissingDefaultFeatureVariant,
                    $"A default variant cannot be found for the feature '{feature}'.");
            }

            IFeatureVariantAssignerMetadata assigner = GetFeatureVariantAssignerMetadata(featureDefinition.Assigner);

            if (assigner == null)
            {
                throw new FeatureManagementException(
                       FeatureManagementError.MissingFeatureVariantAssigner,
                       $"The feature variant assigner '{featureDefinition.Assigner}' specified for feature '{feature}' was not found.");
            }

            var context = new FeatureVariantAssignmentContext()
            {
                FeatureDefinition = featureDefinition
            };

            //
            // IFeatureVariantAssigner
            if (assigner is IFeatureVariantAssigner featureVariantAssigner)
            {
                variant = await featureVariantAssigner.AssignVariantAsync(context, cancellationToken).ConfigureAwait(false);
            }
            //
            // IContextualFeatureVariantAssigner
            else if (useAppContext &&
                     TryGetContextualFeatureVariantAssigner(featureDefinition.Assigner, typeof(TContext), out ContextualFeatureVariantAssignerEvaluator contextualAssigner))
            {
                variant = await contextualAssigner.AssignVariantAsync(context, appContext, cancellationToken).ConfigureAwait(false);
            }
            //
            // The assigner doesn't implement a feature variant assigner interface capable of performing the evaluation
            else
            {
                throw new FeatureManagementException(
                    FeatureManagementError.InvalidFeatureVariantAssigner,
                    useAppContext ?
                        $"The feature variant assigner '{featureDefinition.Assigner}' specified for the feature '{feature}' is not capable of evaluating the requested feature with the provided context." :
                        $"The feature variant assigner '{featureDefinition.Assigner}' specified for the feature '{feature}' is not capable of evaluating the requested feature.");
            }

            if (variant == null)
            {
                variant = defaultVariant;
            }

            return await _variantOptionsResolver.GetOptionsAsync<T>(featureDefinition, variant, cancellationToken).ConfigureAwait(false);
        }

        private async Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext, bool useAppContext, CancellationToken cancellationToken)
        {
            foreach (ISessionManager sessionManager in _sessionManagers)
            {
                bool? readSessionResult = await sessionManager.GetAsync(feature, cancellationToken).ConfigureAwait(false);

                if (readSessionResult.HasValue)
                {
                    return readSessionResult.Value;
                }
            }

            bool enabled = false;

            FeatureFlagDefinition featureDefinition = await _featureDefinitionProvider.GetFeatureFlagDefinitionAsync(feature, cancellationToken).ConfigureAwait(false);

            if (featureDefinition != null)
            {
                //
                // Check if feature is always on
                // If it is, result is true, goto: cache

                if (featureDefinition.EnabledFor.Any(featureFilter => string.Equals(featureFilter.Name, "AlwaysOn", StringComparison.OrdinalIgnoreCase)))
                {
                    enabled = true;
                }
                else
                {
                    //
                    // For all enabling filters listed in the feature's state calculate if they return true
                    // If any executed filters return true, return true

                    foreach (FeatureFilterConfiguration featureFilterConfiguration in featureDefinition.EnabledFor)
                    {
                        if (string.IsNullOrEmpty(featureFilterConfiguration.Name))
                        {
                            throw new FeatureManagementException(
                                FeatureManagementError.MissingFeatureFilter,
                                $"Missing feature filter name for the feature {feature}");
                        }

                        IFeatureFilterMetadata filter = GetFeatureFilterMetadata(featureFilterConfiguration.Name);

                        if (filter == null)
                        {
                            string errorMessage = $"The feature filter '{featureFilterConfiguration.Name}' specified for feature '{feature}' was not found.";

                            if (!_options.IgnoreMissingFeatureFilters)
                            {
                                throw new FeatureManagementException(FeatureManagementError.MissingFeatureFilter, errorMessage);
                            }
                            else
                            {
                                _logger.LogWarning(errorMessage);
                            }

                            continue;
                        }

                        var context = new FeatureFilterEvaluationContext()
                        {
                            FeatureName = featureDefinition.Name,
                            Parameters = featureFilterConfiguration.Parameters
                        };

                        //
                        // IFeatureFilter
                        if (filter is IFeatureFilter featureFilter)
                        {
                            if (await featureFilter.EvaluateAsync(context, cancellationToken).ConfigureAwait(false))
                            {
                                enabled = true;

                                break;
                            }
                        }
                        //
                        // IContextualFeatureFilter
                        else if (useAppContext &&
                                 TryGetContextualFeatureFilter(featureFilterConfiguration.Name, typeof(TContext), out ContextualFeatureFilterEvaluator contextualFilter))
                        {
                            if (await contextualFilter.EvaluateAsync(context, appContext, cancellationToken).ConfigureAwait(false))
                            {
                                enabled = true;

                                break;
                            }
                        }
                        //
                        // The filter doesn't implement a feature filter interface capable of performing the evaluation
                        else
                        {
                            throw new FeatureManagementException(
                                FeatureManagementError.InvalidFeatureFilter,
                                useAppContext ?
                                    $"The feature filter '{featureFilterConfiguration.Name}' specified for the feature '{feature}' is not capable of evaluating the requested feature with the provided context." :
                                    $"The feature filter '{featureFilterConfiguration.Name}' specified for the feature '{feature}' is not capable of evaluating the requested feature.");
                        }
                    }
                }
            }

            foreach (ISessionManager sessionManager in _sessionManagers)
            {
                await sessionManager.SetAsync(feature, enabled, cancellationToken).ConfigureAwait(false);
            }

            return enabled;
        }

        private IFeatureFilterMetadata GetFeatureFilterMetadata(string filterName)
        {
            const string filterSuffix = "filter";

            IFeatureFilterMetadata filter = _filterMetadataCache.GetOrAdd(
                filterName,
                (_) => {

                    IEnumerable<IFeatureFilterMetadata> matchingFilters = _featureFilters.Where(f =>
                    {
                        Type filterType = f.GetType();

                        string name = ((FilterAliasAttribute)Attribute.GetCustomAttribute(filterType, typeof(FilterAliasAttribute)))?.Alias;

                        if (name == null)
                        {
                            name = filterType.Name;
                        }

                        return IsMatchingMetadataName(name, filterName, filterSuffix);
                    });

                    if (matchingFilters.Count() > 1)
                    {
                        throw new FeatureManagementException(FeatureManagementError.AmbiguousFeatureFilter, $"Multiple feature filters match the configured filter named '{filterName}'.");
                    }

                    return matchingFilters.FirstOrDefault();
                }
            );

            return filter;
        }

        private IFeatureVariantAssignerMetadata GetFeatureVariantAssignerMetadata(string assignerName)
        {
            const string assignerSuffix = "assigner";

            IFeatureVariantAssignerMetadata assigner = _assignerMetadataCache.GetOrAdd(
                assignerName,
                (_) => {

                    IEnumerable<IFeatureVariantAssignerMetadata> matchingAssigners = _variantAssigners.Where(a =>
                    {
                        Type assignerType = a.GetType();

                        string name = ((AssignerAliasAttribute)Attribute.GetCustomAttribute(assignerType, typeof(AssignerAliasAttribute)))?.Alias;

                        if (name == null)
                        {
                            name = assignerType.Name;
                        }

                        return IsMatchingMetadataName(name, assignerName, assignerSuffix);
                    });

                    if (matchingAssigners.Count() > 1)
                    {
                        throw new FeatureManagementException(FeatureManagementError.AmbiguousFeatureVariantAssigner, $"Multiple feature variant assigners match the configured assigner named '{assignerName}'.");
                    }

                    return matchingAssigners.FirstOrDefault();
                }
            );

            return assigner;
        }

        private static bool IsMatchingMetadataName(string metadataName, string desiredName, string suffix)
        {
            //
            // Feature filters can be referenced with or without the 'filter' suffix
            // E.g. A feature can reference a filter named 'CustomFilter' as 'Custom' or 'CustomFilter'
            if (!desiredName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
                metadataName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                metadataName = metadataName.Substring(0, metadataName.Length - suffix.Length);
            }

            //
            // Feature filters can have namespaces in their alias
            // If a feature is configured to use a filter without a namespace such as 'MyFilter', then it can match 'MyOrg.MyProduct.MyFilter' or simply 'MyFilter'
            // If a feature is configured to use a filter with a namespace such as 'MyOrg.MyProduct.MyFilter' then it can only match 'MyOrg.MyProduct.MyFilter' 
            if (desiredName.Contains('.'))
            {
                //
                // The configured filter name is namespaced. It must be an exact match.
                return string.Equals(metadataName, desiredName, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                //
                // We take the simple name of a filter, E.g. 'MyFilter' for 'MyOrg.MyProduct.MyFilter'
                string simpleName = metadataName.Contains('.') ? metadataName.Split('.').Last() : metadataName;

                return string.Equals(simpleName, desiredName, StringComparison.OrdinalIgnoreCase);
            }
        }

        private bool TryGetContextualFeatureFilter(string filterName, Type appContextType, out ContextualFeatureFilterEvaluator filter)
        {
            if (appContextType == null)
            {
                throw new ArgumentNullException(nameof(appContextType));
            }

            filter = _contextualFeatureFilterCache.GetOrAdd(
                $"{filterName}{Environment.NewLine}{appContextType.FullName}",
                (_) => {

                    IFeatureFilterMetadata metadata = GetFeatureFilterMetadata(filterName);

                    return ContextualFeatureFilterEvaluator.IsContextualFilter(metadata, appContextType) ?
                        new ContextualFeatureFilterEvaluator(metadata, appContextType) :
                        null;
                }
            );

            return filter != null;
        }

        private bool TryGetContextualFeatureVariantAssigner(string assignerName,  Type appContextType, out ContextualFeatureVariantAssignerEvaluator assigner)
        {
            if (appContextType == null)
            {
                throw new ArgumentNullException(nameof(appContextType));
            }

            assigner = _contextualFeatureVariantAssignerCache.GetOrAdd(
                $"{assignerName}{Environment.NewLine}{appContextType.FullName}",
                (_) => {

                    IFeatureVariantAssignerMetadata metadata = GetFeatureVariantAssignerMetadata(assignerName);

                    return ContextualFeatureVariantAssignerEvaluator.IsContextualVariantAssigner(metadata, appContextType) ?
                        new ContextualFeatureVariantAssignerEvaluator(metadata, appContextType) :
                        null;
                }
            );

            return assigner != null;
        }
    }
}
