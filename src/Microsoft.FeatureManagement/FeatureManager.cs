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
            _filterMetadataCache = new ConcurrentDictionary<string, IFeatureFilterMetadata>();
            _contextualFeatureFilterCache = new ConcurrentDictionary<string, ContextualFeatureFilterEvaluator>();
            _assignerMetadataCache = new ConcurrentDictionary<string, IFeatureVariantAssignerMetadata>();
            _contextualFeatureVariantAssignerCache = new ConcurrentDictionary<string, ContextualFeatureVariantAssignerEvaluator>();
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

            FeatureDefinition featureDefinition = await _featureDefinitionProvider.GetFeatureDefinitionAsync(feature, cancellationToken).ConfigureAwait(false);

            if (featureDefinition != null)
            {
                if (string.IsNullOrEmpty(featureDefinition.Assigner))
                {
                    throw new FeatureManagementException(
                        FeatureManagementError.InvalidConfiguration,
                        $"Invalid feature variant assigner name for the feature {feature}");
                }

                IFeatureVariantAssignerMetadata assigner = GetFeatureAssignerMetadata(featureDefinition.Assigner);

                if (assigner == null)
                {
                    string errorMessage = $"The feature assigner '{featureDefinition.Assigner}' specified for feature '{feature}' was not found.";

                    if (!_options.IgnoreMissingFeatureAssigners)
                    {
                        throw new FeatureManagementException(FeatureManagementError.MissingFeatureAssigner, errorMessage);
                    }
                    else
                    {
                        _logger.LogWarning(errorMessage);

                        return default(T);
                    }
                }

                var context = new FeatureVariantAssignmentContext()
                {
                    FeatureDefinition = featureDefinition
                };

                //
                // IContextualFeatureVariantAssigner
                if (useAppContext)
                {
                    ContextualFeatureVariantAssignerEvaluator contextualAssigner = GetContextualFeatureVariantAssigner(featureDefinition.Assigner, typeof(TContext));

                    if (contextualAssigner != null)
                    {
                        variant = await contextualAssigner.AssignVariantAsync(context, appContext, cancellationToken).ConfigureAwait(false);
                    }
                }

                //
                // IFeatureVariantAssigner
                if (assigner is IFeatureVariantAssigner featureVariantAssigner)
                {
                    variant = await featureVariantAssigner.AssignVariantAsync(context, cancellationToken).ConfigureAwait(false);
                }
            
                if (variant == null &&
                    featureDefinition.Variants != null)
                {
                    variant = featureDefinition.Variants.FirstOrDefault(v => v.Default);
                }
            }

            if (variant == null)
            {
                return default(T);
            }

            if (variant.ConfigurationReference == null)
            {
                throw new FeatureManagementException(
                    FeatureManagementError.MissingConfigurationReference,
                    $"The variant '{variant.Name}' for the feature '{feature}' does not have a configuration reference.");
            }

            return await _variantOptionsResolver.GetOptions<T>(featureDefinition, variant, cancellationToken).ConfigureAwait(false);
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
                                FeatureManagementError.InvalidConfiguration,
                                $"Invalid feature filter name for the feature {feature}");
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
                            FeatureName = feature,
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
            IFeatureFilterMetadata filter = _filterMetadataCache.GetOrAdd(
                filterName,
                (_) => {

                    IEnumerable<IFeatureFilterMetadata> matchingFilters = _featureFilters.Where(f =>
                    {
                        string name = GetMetadataName(f.GetType());

                        return IsMatchingMetadataName(name, filterName);
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

        private IFeatureVariantAssignerMetadata GetFeatureAssignerMetadata(string assignerName)
        {
            IFeatureVariantAssignerMetadata assigner = _assignerMetadataCache.GetOrAdd(
                assignerName,
                (_) => {

                    IEnumerable<IFeatureVariantAssignerMetadata> matchingAssigners = _variantAssigners.Where(f =>
                    {
                        string name = GetMetadataName(f.GetType());

                        return IsMatchingMetadataName(name, assignerName);
                    });

                    if (matchingAssigners.Count() > 1)
                    {
                        throw new FeatureManagementException(FeatureManagementError.AmbiguousFeatureFilter, $"Multiple feature filters match the configured filter named '{assignerName}'.");
                    }

                    return matchingAssigners.FirstOrDefault();
                }
            );

            return assigner;
        }

        private static string GetMetadataName(Type type)
        {
            const string filterSuffix = "filter";
            const string assignerSuffix = "assigner";

            Debug.Assert(type != null);

            string name = ((FilterAliasAttribute)Attribute.GetCustomAttribute(type, typeof(FilterAliasAttribute)))?.Alias;

            if (name == null)
            {
                name = type.Name;

                if (name.EndsWith(filterSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - filterSuffix.Length);
                }
                else if (name.EndsWith(assignerSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - assignerSuffix.Length);
                }
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

        private ContextualFeatureVariantAssignerEvaluator GetContextualFeatureVariantAssigner(string assignerName, Type appContextType)
        {
            if (appContextType == null)
            {
                throw new ArgumentNullException(nameof(appContextType));
            }

            ContextualFeatureVariantAssignerEvaluator assigner = _contextualFeatureVariantAssignerCache.GetOrAdd(
                $"{assignerName}{Environment.NewLine}{appContextType.FullName}",
                (_) => {

                    IFeatureVariantAssignerMetadata metadata = GetFeatureAssignerMetadata(assignerName);

                    return ContextualFeatureVariantAssignerEvaluator.IsContextualFilter(metadata, appContextType) ?
                        new ContextualFeatureVariantAssignerEvaluator(metadata, appContextType) :
                        null;
                }
            );

            return assigner;
        }
    }
}
