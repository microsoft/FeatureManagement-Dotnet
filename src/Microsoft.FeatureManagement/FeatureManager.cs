// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to evaluate whether a feature is enabled or disabled.
    /// </summary>
    class FeatureManager : IFeatureManager
    {
        private delegate Task<bool> FilterEvaluator(FeatureFilterEvaluationContext context, object appContext);
        private readonly IFeatureDefinitionProvider _featureDefinitionProvider;
        private readonly IEnumerable<IFeatureFilterMetadata> _featureFilters;
        private readonly IEnumerable<ISessionManager> _sessionManagers;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<ValueTuple<string, Type>, FilterEvaluator> _evaluatorCache;
        private readonly FeatureManagementOptions _options;

        public FeatureManager(
            IFeatureDefinitionProvider featureDefinitionProvider,
            IEnumerable<IFeatureFilterMetadata> featureFilters,
            IEnumerable<ISessionManager> sessionManagers,
            ILoggerFactory loggerFactory,
            IOptions<FeatureManagementOptions> options)
        {
            _featureDefinitionProvider = featureDefinitionProvider;
            _featureFilters = featureFilters ?? throw new ArgumentNullException(nameof(featureFilters));
            _sessionManagers = sessionManagers ?? throw new ArgumentNullException(nameof(sessionManagers));
            _logger = loggerFactory.CreateLogger<FeatureManager>();
            _evaluatorCache = new ConcurrentDictionary<ValueTuple<string, Type>, FilterEvaluator>();
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Task<bool> IsEnabledAsync(string feature)
        {
            return IsEnabledAsync<object>(feature, null, false);
        }

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext)
        {
            return IsEnabledAsync(feature, appContext, true);
        }

        public async IAsyncEnumerable<string> GetFeatureNamesAsync()
        {
            await foreach (FeatureDefinition featureDefintion in _featureDefinitionProvider.GetAllFeatureDefinitionsAsync().ConfigureAwait(false))
            {
                yield return featureDefintion.Name;
            }
        }

        private static bool IsFilterNameMatch(Type filterType, string filterName)
        {
            const string filterSuffix = "filter";
            string name = ((FilterAliasAttribute)Attribute.GetCustomAttribute(filterType, typeof(FilterAliasAttribute)))?.Alias;
            if (name == null)
            {
                name = filterType.Name.EndsWith(filterSuffix, StringComparison.OrdinalIgnoreCase)
                    ? filterType.Name.Substring(0, filterType.Name.Length - filterSuffix.Length) : filterType.Name;
            }

            //
            // Feature filters can have namespaces in their alias
            // If a feature is configured to use a filter without a namespace such as 'MyFilter', then it can match 'MyOrg.MyProduct.MyFilter' or simply 'MyFilter'
            // If a feature is configured to use a filter with a namespace such as 'MyOrg.MyProduct.MyFilter' then it can only match 'MyOrg.MyProduct.MyFilter' 
            if (filterName.Contains('.'))
            {
                //
                // The configured filter name is namespaced. It must be an exact match.
                return string.Equals(name, filterName, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                //
                // We take the simple name of a filter, E.g. 'MyFilter' for 'MyOrg.MyProduct.MyFilter'
                int dotIndex = name.LastIndexOf('.');
                string simpleName = dotIndex != -1 ? name.Substring(dotIndex + 1) : name;
                return string.Equals(simpleName, filterName, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static ContextualFeatureFilterEvaluator GetContextualFeatureFilter(IFeatureFilterMetadata metadata, Type appContextType)
        {
            if (appContextType == null)
            {
                throw new ArgumentNullException(nameof(appContextType));
            }

            return ContextualFeatureFilterEvaluator.IsContextualFilter(metadata, appContextType)
                ? new ContextualFeatureFilterEvaluator(metadata, appContextType) : null;
        }

        private async Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext, bool useAppContext)
        {
            foreach (ISessionManager sessionManager in _sessionManagers)
            {
                bool? readSessionResult = await sessionManager.GetAsync(feature).ConfigureAwait(false);

                if (readSessionResult.HasValue)
                {
                    return readSessionResult.Value;
                }
            }

            bool enabled = false;

            FeatureDefinition featureDefinition = await _featureDefinitionProvider.GetFeatureDefinitionAsync(feature).ConfigureAwait(false);

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
                        FilterEvaluator filter = GetFeatureFilterEvaluator(
                            featureFilterConfiguration.Name, useAppContext ? typeof(TContext) : null);

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

                        enabled = await filter.Invoke(context, appContext).ConfigureAwait(false);
                        if (enabled)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                string errorMessage = $"The feature declaration for the feature '{feature}' was not found.";

                if (!_options.IgnoreMissingFeatures)
                {
                    throw new FeatureManagementException(FeatureManagementError.MissingFeature, errorMessage);
                }
                else
                {
                    _logger.LogWarning(errorMessage);
                }
            }

            foreach (ISessionManager sessionManager in _sessionManagers)
            {
                await sessionManager.SetAsync(feature, enabled).ConfigureAwait(false);
            }

            return enabled;
        }

        private FilterEvaluator GetFeatureFilterEvaluator(
            string filterName, Type appContextType)
        {
            //
            // We will support having multiple filters with the same alias if they all implement different
            // IFeatureFilterMetadata interfaces. More explicitly, for a given filter alias, you can have:
            // 0 or 1 IFeatureFilter implementations
            // 0 or N IContextualFeatureFilter<T> implementations, so long as <T> is assignable to only 1
            // discovered contextual filter (so no IContextFeatureFilter<T> and IContextFeatureFilter<interface of T>).
            return _evaluatorCache.GetOrAdd((filterName, appContextType), key =>
            {
                static void ThrowAmbiguousFeatureFilter(string filterName, Type appContextType = null)
                {
                    if (appContextType is null)
                    {
                        throw new FeatureManagementException(
                            FeatureManagementError.AmbiguousFeatureFilter,
                            $"Multiple feature filters match the configured filter named '{filterName}'.");
                    }

                    throw new FeatureManagementException(
                        FeatureManagementError.AmbiguousFeatureFilter,
                        $"Multiple contextual feature filters match the configured filter named '{filterName}'"
                        + $" and app context '{appContextType}'.");
                }

                (string filterName, Type appContextType) = key;
                bool foundOneMatch = false;
                IFeatureFilter filter = null;
                ContextualFeatureFilterEvaluator contextualEvaluator = null;
                foreach (IFeatureFilterMetadata metadata in _featureFilters)
                {
                    Type t = metadata.GetType();
                    if (!IsFilterNameMatch(t, filterName))
                    {
                        continue;
                    }

                    if (!_options.AllowDuplicateContextualAlias && foundOneMatch)
                    {
                        // Retain existing behavior of throwing when two aliases match, regardless of if the
                        // contextual evaluation is applicable.
                        ThrowAmbiguousFeatureFilter(filterName);
                    }

                    foundOneMatch = true;
                    if (metadata is IFeatureFilter f)
                    {
                        if (filter is object)
                        {
                            // More than 1 matching IFeatureFilter.
                            ThrowAmbiguousFeatureFilter(filterName);
                        }

                        _logger.LogDebug("Filter {FilterType} matched {FilterName} as IFeatureFilter.", t, filterName);
                        filter = f;
                        continue;
                    }

                    if (appContextType is object)
                    {
                        ContextualFeatureFilterEvaluator evaluator = GetContextualFeatureFilter(
                            metadata, appContextType);
                        if (contextualEvaluator is null)
                        {
                            _logger.LogDebug(
                                "Filter {FilterType} matched {FilterName} as IContextualFeatureFilter.", t, filterName);
                            contextualEvaluator = evaluator;
                        }
                        else if (evaluator is object)
                        {
                            // More than 1 matching IContextualFeatureFilter
                            ThrowAmbiguousFeatureFilter(filterName, appContextType);
                        }

                        continue;
                    }

                    _logger.LogTrace("Filter {FilterType} matched {FilterName} but not app context.", t, filterName);
                }

                if (contextualEvaluator is object)
                {
                    // Only present when appContextType is not null.
                    return (context, appContext) => contextualEvaluator.EvaluateAsync(context, appContext);
                }
                else if (filter is object)
                {
                    // When appContextType is null or there was no matching contextual filter.
                    return (context, appContext) => filter.EvaluateAsync(context);
                }
                else if (foundOneMatch)
                {
                    // This block means we found an incompatible contextual evaluator. To retain the existing
                    // behavior, we need to return a constant false evaluation here.
                    return (context, appContext) => Task.FromResult(false);
                }

                return null;
            });
        }
    }
}
