// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to evaluate whether a feature is enabled or disabled.
    /// </summary>
    class FeatureManager : IFeatureManager, IFeatureVariantManager
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

        public async IAsyncEnumerable<string> GetFeatureNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (FeatureDefinition featureDefintion in _featureDefinitionProvider.GetAllFeatureDefinitionsAsync(cancellationToken).ConfigureAwait(false))
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

            FeatureDefinition featureDefinition = await _featureDefinitionProvider
                .GetFeatureDefinitionAsync(feature, cancellationToken)
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
                    FeatureManagementError.MissingVariants,
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
                            FeatureManagementError.AmbiguousDefaultVariant,
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
                    FeatureManagementError.MissingDefaultVariant,
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

            if (!useAppContext)
            {
                if (assigner is IFeatureVariantAssigner featureVariantAssigner)
                {
                    variant = await featureVariantAssigner.AssignVariantAsync(context, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw new FeatureManagementException(
                        FeatureManagementError.InvalidFeatureVariantAssigner,
                        $"The feature variant assigner '{featureDefinition.Assigner}' specified for feature '{feature}' is not capable of evaluating the requested feature without a provided context.");
                }
            }
            else
            {
                ContextualFeatureVariantAssignerEvaluator contextualAssigner = GetContextualFeatureVariantAssigner(
                    featureDefinition.Assigner,
                    typeof(TContext));

                if (contextualAssigner != null)
                {
                    variant = await contextualAssigner.AssignVariantAsync(context, appContext, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw new FeatureManagementException(
                        FeatureManagementError.InvalidFeatureVariantAssigner,
                        $"The feature variant assigner '{featureDefinition.Assigner}' specified for feature '{feature}' is not capable of evaluating the requested feature with the provided context.");
                }
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

            FeatureDefinition featureDefinition = await _featureDefinitionProvider.GetFeatureDefinitionAsync(feature, cancellationToken).ConfigureAwait(false);

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
                        // IContextualFeatureFilter
                        if (useAppContext)
                        {
                            ContextualFeatureFilterEvaluator contextualFilter = GetContextualFeatureFilter(featureFilterConfiguration.Name, typeof(TContext));

                            if (contextualFilter != null && await contextualFilter.EvaluateAsync(context, appContext, cancellationToken).ConfigureAwait(false))
                            {
                                enabled = true;

                                break;
                            }
                        }

                        //
                        // IFeatureFilter
                        if (filter is IFeatureFilter featureFilter && await featureFilter.EvaluateAsync(context, cancellationToken).ConfigureAwait(false))
                        {
                            enabled = true;

                            break;
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

                        return filterName.EndsWith(filterSuffix, StringComparison.OrdinalIgnoreCase) ?
                            IsMatchingMetadataName(name, filterName) :
                            IsMatchingMetadataName(GetTrimmedName(name, filterSuffix), filterName);
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
                        Type filterType = a.GetType();

                        string name = ((AssignerAliasAttribute)Attribute.GetCustomAttribute(filterType, typeof(AssignerAliasAttribute)))?.Alias;

                        if (name == null)
                        {
                            name = filterType.Name;
                        }

                        return assignerName.EndsWith(assignerSuffix, StringComparison.OrdinalIgnoreCase) ?
                            IsMatchingMetadataName(name, assignerName) :
                            IsMatchingMetadataName(GetTrimmedName(name, assignerSuffix), assignerName);
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

        /// <summary>
        /// Trims a suffix from a name if necessary
        /// </summary>
        /// <param name="name">The name to trim.</param>
        /// <param name="suffix">The possible suffix that may need trimming.</param>
        /// <returns></returns>
        private static string GetTrimmedName(string name, string suffix)
        {
            Debug.Assert(name != null);
            Debug.Assert(suffix != null);

            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - suffix.Length);
            }

            return name;
        }

        private static bool IsMatchingMetadataName(string metadataName, string desiredName)
        {
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

        private ContextualFeatureFilterEvaluator GetContextualFeatureFilter(string filterName, Type appContextType)
        {
            if (appContextType == null)
            {
                throw new ArgumentNullException(nameof(appContextType));
            }

            ContextualFeatureFilterEvaluator filter = _contextualFeatureFilterCache.GetOrAdd(
                $"{filterName}{Environment.NewLine}{appContextType.FullName}",
                (_) => {

                    IFeatureFilterMetadata metadata = GetFeatureFilterMetadata(filterName);

                    return ContextualFeatureFilterEvaluator.IsContextualFilter(metadata, appContextType) ?
                        new ContextualFeatureFilterEvaluator(metadata, appContextType) :
                        null;
                }
            );

            return filter;
        }

        private ContextualFeatureVariantAssignerEvaluator GetContextualFeatureVariantAssigner(string assignerName,  Type appContextType)
        {
            if (appContextType == null)
            {
                throw new ArgumentNullException(nameof(appContextType));
            }

            ContextualFeatureVariantAssignerEvaluator assigner = _contextualFeatureVariantAssignerCache.GetOrAdd(
                $"{assignerName}{Environment.NewLine}{appContextType.FullName}",
                (_) => {

                    IFeatureVariantAssignerMetadata metadata = GetFeatureVariantAssignerMetadata(assignerName);

                    return ContextualFeatureVariantAssignerEvaluator.IsContextualVariantAssigner(metadata, appContextType) ?
                        new ContextualFeatureVariantAssignerEvaluator(metadata, appContextType) :
                        null;
                }
            );

            return assigner;
        }
    }
}
