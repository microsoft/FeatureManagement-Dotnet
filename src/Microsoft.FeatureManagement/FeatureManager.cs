// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    public sealed class FeatureManager : IFeatureManager
    {
        private readonly TimeSpan ParametersCacheSlidingExpiration = TimeSpan.FromMinutes(5);
        private readonly TimeSpan ParametersCacheAbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

        private readonly IFeatureDefinitionProvider _featureDefinitionProvider;
        private readonly IEnumerable<IFeatureFilterMetadata> _featureFilters;
        private readonly IEnumerable<ISessionManager> _sessionManagers;
        private readonly ConcurrentDictionary<string, IFeatureFilterMetadata> _filterMetadataCache;
        private readonly ConcurrentDictionary<string, ContextualFeatureFilterEvaluator> _contextualFeatureFilterCache;
        private readonly FeatureManagementOptions _options;

        private class ConfigurationCacheItem
        {
            public IConfiguration Parameters { get; set; }

            public object Settings { get; set; }
        }

        /// <summary>
        /// Creates a feature manager.
        /// </summary>
        /// <param name="featureDefinitionProvider">The provider of feature flag definitions.</param>
        /// <param name="options">Options controlling the behavior of the feature manager.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureDefinitionProvider"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        public FeatureManager(
            IFeatureDefinitionProvider featureDefinitionProvider,
            FeatureManagementOptions options)
        {
            _filterMetadataCache = new ConcurrentDictionary<string, IFeatureFilterMetadata>();
            _contextualFeatureFilterCache = new ConcurrentDictionary<string, ContextualFeatureFilterEvaluator>();
            _featureDefinitionProvider = featureDefinitionProvider ?? throw new ArgumentNullException(nameof(featureDefinitionProvider));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _featureFilters = Enumerable.Empty<IFeatureFilterMetadata>();
            _sessionManagers = Enumerable.Empty<ISessionManager>();
        }

        /// <summary>
        /// The collection of feature filter metadata.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if it is set to null.</exception>
        public IEnumerable<IFeatureFilterMetadata> FeatureFilters
        {
            get => _featureFilters;

            init
            {
                _featureFilters = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// The collection of session managers.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if it is set to null.</exception>
        public IEnumerable<ISessionManager> SessionManagers
        {
            get => _sessionManagers;

            init
            {
                _sessionManagers = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// The application memory cache to store feature filter settings.
        /// </summary>
        public IMemoryCache Cache { get; init; }

        /// <summary>
        /// The logger for the feature manager.
        /// </summary>
        public ILogger Logger { get; init; }

        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        public Task<bool> IsEnabledAsync(string feature)
        {
            return IsEnabledAsync<object>(feature, null, false);
        }

        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="appContext">A context providing information that can be used to evaluate whether a feature should be on or off.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext)
        {
            return IsEnabledAsync(feature, appContext, true);
        }

        /// <summary>
        /// Retrieves a list of feature names registered in the feature manager.
        /// </summary>
        /// <returns>An enumerator which provides asynchronous iteration over the feature names registered in the feature manager.</returns>
        public async IAsyncEnumerable<string> GetFeatureNamesAsync()
        {
            await foreach (FeatureDefinition featureDefintion in _featureDefinitionProvider.GetAllFeatureDefinitionsAsync().ConfigureAwait(false))
            {
                yield return featureDefintion.Name;
            }
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

            bool enabled;

            FeatureDefinition featureDefinition = await _featureDefinitionProvider.GetFeatureDefinitionAsync(feature).ConfigureAwait(false);

            if (featureDefinition != null)
            {
                if (featureDefinition.RequirementType == RequirementType.All && _options.IgnoreMissingFeatureFilters)
                {
                    throw new FeatureManagementException(
                        FeatureManagementError.Conflict, 
                        $"The 'IgnoreMissingFeatureFilters' flag cannot use used in combination with a feature of requirement type 'All'.");
                }

                //
                // Treat an empty list of enabled filters as a disabled feature
                if (featureDefinition.EnabledFor == null || !featureDefinition.EnabledFor.Any())
                {
                    enabled = false;
                }
                else
                {
                    //
                    // If the requirement type is all, we default to true. Requirement type All will end on a false
                    enabled = featureDefinition.RequirementType == RequirementType.All;

                    //
                    // We iterate until we hit our target evaluation
                    bool targetEvaluation = !enabled;

                    //
                    // Keep track of the index of the filter we are evaluating
                    int filterIndex = -1;

                    //
                    // For all enabling filters listed in the feature's state, evaluate them according to requirement type
                    foreach (FeatureFilterConfiguration featureFilterConfiguration in featureDefinition.EnabledFor)
                    {
                        filterIndex++;

                        //
                        // Handle AlwaysOn filters
                        if (string.Equals(featureFilterConfiguration.Name, "AlwaysOn", StringComparison.OrdinalIgnoreCase))
                        {
                            if (featureDefinition.RequirementType == RequirementType.Any)
                            {
                                enabled = true;
                                break;
                            }
                            
                            continue;
                        }

                        IFeatureFilterMetadata filter;

                        if (useAppContext)
                        {
                            filter = GetFeatureFilterMetadata(featureFilterConfiguration.Name, typeof(TContext)) ??
                                     GetFeatureFilterMetadata(featureFilterConfiguration.Name);
                        }
                        else
                        {
                            filter = GetFeatureFilterMetadata(featureFilterConfiguration.Name);
                        }

                        if (filter == null)
                        {
                            if (_featureFilters.Any(f => IsMatchingName(f.GetType(), featureFilterConfiguration.Name)))
                            {
                                //
                                // Cannot find the appropriate registered feature filter which matches the filter name and the provided context type.
                                // But there is a registered feature filter which matches the filter name.
                                continue;
                            }

                            string errorMessage = $"The feature filter '{featureFilterConfiguration.Name}' specified for feature '{feature}' was not found.";

                            if (!_options.IgnoreMissingFeatureFilters)
                            {
                                throw new FeatureManagementException(FeatureManagementError.MissingFeatureFilter, errorMessage);
                            }

                            Logger?.LogWarning(errorMessage);

                            continue;
                        }

                        var context = new FeatureFilterEvaluationContext()
                        {
                            FeatureName = feature,
                            Parameters = featureFilterConfiguration.Parameters
                        };

                        BindSettings(filter, context, filterIndex);

                        //
                        // IContextualFeatureFilter
                        if (useAppContext)
                        {
                            ContextualFeatureFilterEvaluator contextualFilter = GetContextualFeatureFilter(featureFilterConfiguration.Name, typeof(TContext));

                            if (contextualFilter != null &&
                                await contextualFilter.EvaluateAsync(context, appContext).ConfigureAwait(false) == targetEvaluation)
                            {
                                enabled = targetEvaluation;

                                break;
                            }
                        }

                        //
                        // IFeatureFilter
                        if (filter is IFeatureFilter featureFilter)
                        {
                            if (await featureFilter.EvaluateAsync(context).ConfigureAwait(false) == targetEvaluation)
                            {
                                enabled = targetEvaluation;

                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                enabled = false;

                string errorMessage = $"The feature definition for the feature '{feature}' was not found.";

                if (!_options.IgnoreMissingFeatures)
                {
                    throw new FeatureManagementException(FeatureManagementError.MissingFeature, errorMessage);
                }
                
                Logger?.LogDebug(errorMessage);
            }

            foreach (ISessionManager sessionManager in _sessionManagers)
            {
                await sessionManager.SetAsync(feature, enabled).ConfigureAwait(false);
            }

            return enabled;
        }

        private void BindSettings(IFeatureFilterMetadata filter, FeatureFilterEvaluationContext context, int filterIndex)
        {
            IFilterParametersBinder binder = filter as IFilterParametersBinder;

            if (binder == null)
            {
                return;
            }

            if (!(_featureDefinitionProvider is IFeatureDefinitionProviderCacheable) || Cache == null)
            {
                context.Settings = binder.BindParameters(context.Parameters);

                return;
            }

            object settings;

            ConfigurationCacheItem cacheItem;

            string cacheKey = $"{context.FeatureName}.{filterIndex}";

            //
            // Check if settings already bound from configuration or the parameters have changed
            if (!Cache.TryGetValue(cacheKey, out cacheItem) ||
                cacheItem.Parameters != context.Parameters)
            {
                settings = binder.BindParameters(context.Parameters);

                Cache.Set(
                    cacheKey,
                    new ConfigurationCacheItem
                    {
                        Settings = settings,
                        Parameters = context.Parameters
                    },
                    new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = ParametersCacheSlidingExpiration,
                        AbsoluteExpirationRelativeToNow = ParametersCacheAbsoluteExpirationRelativeToNow,
                        Size = 1
                    });
            }
            else
            {
                settings = cacheItem.Settings;
            }

            context.Settings = settings;
        }

        private IFeatureFilterMetadata GetFeatureFilterMetadata(string filterName, Type appContextType = null)
        {
            IFeatureFilterMetadata filter = _filterMetadataCache.GetOrAdd(
                $"{filterName}{Environment.NewLine}{appContextType?.FullName}",
                (_) => {

                    IEnumerable<IFeatureFilterMetadata> matchingFilters = _featureFilters.Where(f =>
                    {
                        Type filterType = f.GetType();

                        if (!IsMatchingName(filterType, filterName))
                        {
                            return false;
                        }

                        if (appContextType == null)
                        {
                            return (f is IFeatureFilter);
                        }

                        return ContextualFeatureFilterEvaluator.IsContextualFilter(f, appContextType);
                    });

                    if (matchingFilters.Count() > 1)
                    {
                        if (appContextType == null)
                        {
                            throw new FeatureManagementException(FeatureManagementError.AmbiguousFeatureFilter, $"Multiple feature filters match the configured filter named '{filterName}'.");
                        }
                        else
                        {
                            throw new FeatureManagementException(FeatureManagementError.AmbiguousFeatureFilter, $"Multiple contextual feature filters match the configured filter named '{filterName}' and context type '{appContextType}'.");
                        }
                    }

                    return matchingFilters.FirstOrDefault();
                }
            );

            return filter;
        }

        private bool IsMatchingName(Type filterType, string filterName)
        {
            const string filterSuffix = "filter";

            string name = ((FilterAliasAttribute)Attribute.GetCustomAttribute(filterType, typeof(FilterAliasAttribute)))?.Alias;

            if (name == null)
            {
                name = filterType.Name.EndsWith(filterSuffix, StringComparison.OrdinalIgnoreCase) ? filterType.Name.Substring(0, filterType.Name.Length - filterSuffix.Length) : filterType.Name;
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
                string simpleName = name.Contains('.') ? name.Split('.').Last() : name;

                return string.Equals(simpleName, filterName, StringComparison.OrdinalIgnoreCase);
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

                    IFeatureFilterMetadata metadata = GetFeatureFilterMetadata(filterName, appContextType);

                    if (metadata == null)
                    {
                        return null;
                    }

                    return new ContextualFeatureFilterEvaluator(metadata, appContextType);
                }
            );

            return filter;
        }
    }
}
