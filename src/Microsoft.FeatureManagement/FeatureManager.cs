// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
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
    class FeatureManager : IFeatureManager, IDisposable
    {
        private readonly TimeSpan ParametersCacheSlidingExpiration = TimeSpan.FromMinutes(5);
        private readonly TimeSpan ParametersCacheAbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

        private readonly IFeatureDefinitionProvider _featureDefinitionProvider;
        private readonly IEnumerable<IFeatureFilterMetadata> _featureFilters;
        private readonly IEnumerable<ISessionManager> _sessionManagers;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, IFeatureFilterMetadata> _filterMetadataCache;
        private readonly ConcurrentDictionary<string, ContextualFeatureFilterEvaluator> _contextualFeatureFilterCache;
        private readonly FeatureManagementOptions _options;
        private readonly IMemoryCache _parametersCache;
        
        private class ConfigurationCacheItem
        {
            public IConfiguration Parameters { get; set; }

            public object Settings { get; set; }
        }

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
            _filterMetadataCache = new ConcurrentDictionary<string, IFeatureFilterMetadata>();
            _contextualFeatureFilterCache = new ConcurrentDictionary<string, ContextualFeatureFilterEvaluator>();
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _parametersCache = new MemoryCache(new MemoryCacheOptions());
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

        public void Dispose()
        {
            _parametersCache.Dispose();
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

                        IFeatureFilterMetadata filter = GetFeatureFilterMetadata(featureFilterConfiguration.Name);

                        if (filter == null)
                        {
                            string errorMessage = $"The feature filter '{featureFilterConfiguration.Name}' specified for feature '{feature}' was not found.";

                            if (!_options.IgnoreMissingFeatureFilters)
                            {
                                throw new FeatureManagementException(FeatureManagementError.MissingFeatureFilter, errorMessage);
                            }

                            _logger.LogWarning(errorMessage);

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

                            BindSettings(filter, context, filterIndex);

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
                            BindSettings(filter, context, filterIndex);

                            if (await featureFilter.EvaluateAsync(context).ConfigureAwait(false) == targetEvaluation) {
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

                string errorMessage = $"The feature declaration for the feature '{feature}' was not found.";

                if (!_options.IgnoreMissingFeatures)
                {
                    throw new FeatureManagementException(FeatureManagementError.MissingFeature, errorMessage);
                }
                
                _logger.LogWarning(errorMessage);
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

            if (!(_featureDefinitionProvider is IFeatureDefinitionProviderCacheable))
            {
                context.Settings = binder.BindParameters(context.Parameters);

                return;
            }

            object settings;

            ConfigurationCacheItem cacheItem;

            string cacheKey = $"{context.FeatureName}.{filterIndex}";

            //
            // Check if settings already bound from configuration or the parameters have changed
            if (!_parametersCache.TryGetValue(cacheKey, out cacheItem) ||
                cacheItem.Parameters != context.Parameters)
            {
                settings = binder.BindParameters(context.Parameters);

                _parametersCache.Set(
                    cacheKey,
                    new ConfigurationCacheItem
                    {
                        Settings = settings,
                        Parameters = context.Parameters
                    },
                    new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = ParametersCacheSlidingExpiration,
                        AbsoluteExpirationRelativeToNow = ParametersCacheAbsoluteExpirationRelativeToNow
                    });
            }
            else
            {
                settings = cacheItem.Settings;
            }

            context.Settings = settings;
        }

        private IFeatureFilterMetadata GetFeatureFilterMetadata(string filterName)
        {
            const string filterSuffix = "filter";

            IFeatureFilterMetadata filter = _filterMetadataCache.GetOrAdd(
                filterName,
                (_) => {

                    IEnumerable<IFeatureFilterMetadata> matchingFilters = _featureFilters.Where(f =>
                    {
                        Type t = f.GetType();

                        string name = ((FilterAliasAttribute)Attribute.GetCustomAttribute(t, typeof(FilterAliasAttribute)))?.Alias;

                        if (name == null)
                        {
                            name = t.Name.EndsWith(filterSuffix, StringComparison.OrdinalIgnoreCase) ? t.Name.Substring(0, t.Name.Length - filterSuffix.Length) : t.Name;
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
    }
}
