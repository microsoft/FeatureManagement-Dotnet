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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to evaluate whether a feature is enabled or disabled.
    /// </summary>
    class FeatureManager : IFeatureManager, IDisposable
    {
        private readonly TimeSpan SettingsCachePeriod = TimeSpan.FromSeconds(5);
        private readonly IFeatureFlagDefinitionProvider _featureDefinitionProvider;
        private readonly IEnumerable<IFeatureFilterMetadata> _featureFilters;
        private readonly IEnumerable<ISessionManager> _sessionManagers;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, IFeatureFilterMetadata> _filterMetadataCache;
        private readonly ConcurrentDictionary<string, ContextualFeatureFilterEvaluator> _contextualFeatureFilterCache;
        private readonly FeatureManagementOptions _options;
        private readonly IMemoryCache _cache;

        private class ConfigurationCacheItem
        {
            public IConfiguration Parameters { get; set; }

            public object Settings { get; set; }
        }

        public FeatureManager(
            IFeatureFlagDefinitionProvider featureDefinitionProvider,
            IEnumerable<IFeatureFilterMetadata> featureFilters,
            IEnumerable<ISessionManager> sessionManagers,
            ILoggerFactory loggerFactory,
            IOptions<FeatureManagementOptions> options)
        {
            _featureFilters = featureFilters ?? throw new ArgumentNullException(nameof(featureFilters));
            _featureDefinitionProvider = featureDefinitionProvider ?? throw new ArgumentNullException(nameof(featureDefinitionProvider));
            _sessionManagers = sessionManagers ?? throw new ArgumentNullException(nameof(sessionManagers));
            _logger = loggerFactory.CreateLogger<FeatureManager>();
            _filterMetadataCache = new ConcurrentDictionary<string, IFeatureFilterMetadata>(StringComparer.OrdinalIgnoreCase);
            _contextualFeatureFilterCache = new ConcurrentDictionary<string, ContextualFeatureFilterEvaluator>(StringComparer.OrdinalIgnoreCase);
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _cache = new MemoryCache(
                Options.Create(
                    new MemoryCacheOptions
                    {
                        ExpirationScanFrequency = SettingsCachePeriod
                    }));
        }

        public Task<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            return IsEnabledAsync<object>(feature, null, false, cancellationToken);
        }

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            return IsEnabledAsync(feature, appContext, true, cancellationToken);
        }

        public async IAsyncEnumerable<string> GetFeatureFlagNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (FeatureFlagDefinition featureDefintion in _featureDefinitionProvider.GetAllFeatureFlagDefinitionsAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return featureDefintion.Name;
            }
        }

        public void Dispose()
        {
            _cache.Dispose();
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
                            FeatureFlagName = featureDefinition.Name,
                            Parameters = featureFilterConfiguration.Parameters
                        };

                        //
                        // IFeatureFilter
                        if (filter is IFeatureFilter featureFilter)
                        {
                            BindSettings(filter, context);

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
                            BindSettings(filter, context);

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

                        return NameHelper.IsMatchingReference(
                            reference: filterName,
                            metadataName: name,
                            suffix: filterSuffix);
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

        private void BindSettings(IFeatureFilterMetadata filter, FeatureFilterEvaluationContext context)
        {
            IFilterParametersBinder binder = filter as IFilterParametersBinder;

            if (binder == null)
            {
                return;
            }

            object settings;

            //
            // Check if settings already bound from configuration
            ConfigurationCacheItem cacheItem = (ConfigurationCacheItem)_cache.Get(context.FeatureFlagName);

            if (cacheItem == null ||
                cacheItem.Parameters != context.Parameters)
            {
                settings = binder.BindParameters(context.Parameters);

                _cache.Set(
                    context.FeatureFlagName,
                    new ConfigurationCacheItem
                    {
                        Settings = settings,
                        Parameters = context.Parameters
                    },
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = SettingsCachePeriod
                    });
            }
            else
            {
                settings = cacheItem.Settings;
            }

            context.Settings = settings;
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
    }
}
