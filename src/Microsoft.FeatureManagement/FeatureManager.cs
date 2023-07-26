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
    class FeatureManager : IFeatureManager, IDisposable, IVariantFeatureManager
    {
        private readonly TimeSpan ParametersCacheSlidingExpiration = TimeSpan.FromMinutes(5);
        private readonly TimeSpan ParametersCacheAbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

        private readonly IFeatureDefinitionProvider _featureDefinitionProvider;
        private readonly IEnumerable<IFeatureFilterMetadata> _featureFilters;
        private readonly IEnumerable<ISessionManager> _sessionManagers;
        private readonly IEnumerable<IFeatureVariantAllocatorMetadata> _variantAllocators;
        private readonly ConcurrentDictionary<string, IFeatureVariantAllocatorMetadata> _allocatorMetadataCache;
        private readonly ConcurrentDictionary<string, ContextualFeatureVariantAllocatorEvaluator> _contextualFeatureVariantAllocatorCache;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, IFeatureFilterMetadata> _filterMetadataCache;
        private readonly ConcurrentDictionary<string, ContextualFeatureFilterEvaluator> _contextualFeatureFilterCache;
        private readonly IFeatureVariantOptionsResolver _variantOptionsResolver;
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
            IEnumerable<IFeatureVariantAllocatorMetadata> variantAllocators,
            IFeatureVariantOptionsResolver variantOptionsResolver,
            ILoggerFactory loggerFactory,
            IOptions<FeatureManagementOptions> options)
        {
            _featureDefinitionProvider = featureDefinitionProvider;
            _featureFilters = featureFilters ?? throw new ArgumentNullException(nameof(featureFilters));
            _sessionManagers = sessionManagers ?? throw new ArgumentNullException(nameof(sessionManagers));
            _variantAllocators = variantAllocators ?? throw new ArgumentNullException(nameof(variantAllocators));
            _variantOptionsResolver = variantOptionsResolver ?? throw new ArgumentNullException(nameof(variantOptionsResolver));
            _allocatorMetadataCache = new ConcurrentDictionary<string, IFeatureVariantAllocatorMetadata>(StringComparer.OrdinalIgnoreCase);
            _contextualFeatureVariantAllocatorCache = new ConcurrentDictionary<string, ContextualFeatureVariantAllocatorEvaluator>(StringComparer.OrdinalIgnoreCase);
            _logger = loggerFactory.CreateLogger<FeatureManager>();
            _filterMetadataCache = new ConcurrentDictionary<string, IFeatureFilterMetadata>();
            _contextualFeatureFilterCache = new ConcurrentDictionary<string, ContextualFeatureFilterEvaluator>();
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _parametersCache = new MemoryCache(new MemoryCacheOptions());
        }

        public Task<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken)
        {
            return IsEnabledAsync<object>(feature, null, false, false, cancellationToken);
        }

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext, CancellationToken cancellationToken)
        {
            return IsEnabledAsync(feature, appContext, true, false, cancellationToken);
        }

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext, bool ignoreVariant, CancellationToken cancellationToken)
        {
            return IsEnabledAsync(feature, appContext, true, ignoreVariant, cancellationToken);
        }

        public async IAsyncEnumerable<string> GetFeatureNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (FeatureDefinition featureDefintion in _featureDefinitionProvider
                                .GetAllFeatureDefinitionsAsync(cancellationToken)
                                .ConfigureAwait(false))
            {
                yield return featureDefintion.Name;
            }
        }

        public void Dispose()
        {
            _parametersCache.Dispose();
        }

        private async Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext, bool useAppContext, bool ignoreVariant, CancellationToken cancellationToken)
        {
            foreach (ISessionManager sessionManager in _sessionManagers)
            {
                bool? readSessionResult = await sessionManager.GetAsync(feature, cancellationToken).ConfigureAwait(false);

                if (readSessionResult.HasValue)
                {
                    return readSessionResult.Value;
                }
            }

            bool enabled;

            FeatureDefinition featureDefinition = await _featureDefinitionProvider
                .GetFeatureDefinitionAsync(feature, cancellationToken)
                .ConfigureAwait(false);

            if (featureDefinition != null)
            {
                if (featureDefinition.RequirementType == RequirementType.All && _options.IgnoreMissingFeatureFilters)
                {
                    throw new FeatureManagementException(
                        FeatureManagementError.Conflict, 
                        $"The 'IgnoreMissingFeatureFilters' flag cannot use used in combination with a feature of requirement type 'All'.");
                }

                //
                // Treat an empty list of enabled filters or if status is disabled as a disabled feature
                if (featureDefinition.EnabledFor == null || !featureDefinition.EnabledFor.Any() || featureDefinition.Status == Status.Disabled)
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

                        ////
                        //// Handle On filters for variants
                        //if (string.Equals(featureFilterConfiguration.Name, "On", StringComparison.OrdinalIgnoreCase))
                        //{
                        //    // TODO
                        //}

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
                                await contextualFilter.EvaluateAsync(context, appContext, cancellationToken).ConfigureAwait(false) == targetEvaluation)
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

                            if (await featureFilter.EvaluateAsync(context, cancellationToken).ConfigureAwait(false) == targetEvaluation)
                            {
                                enabled = targetEvaluation;

                                break;
                            }
                        }
                    }
                }
                
                if (!ignoreVariant && (featureDefinition.Variants?.Any() ?? false))
                {
                    FeatureVariant featureVariant = await GetFeatureVariantAsync(feature, featureDefinition, appContext, useAppContext, enabled, cancellationToken);

                    if (featureVariant != null)
                    {
                        if (featureVariant.StatusOverride == StatusOverride.Enabled)
                        {
                            enabled = true;
                        }
                        else if (featureVariant.StatusOverride == StatusOverride.Disabled)
                        {
                            enabled = false;
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
                await sessionManager.SetAsync(feature, enabled, cancellationToken).ConfigureAwait(false);
            }

            return enabled;
        }

        public ValueTask<Variant> GetVariantAsync(string feature, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            return GetVariantAsync<object>(feature, null, false, cancellationToken);
        }

        public ValueTask<Variant> GetVariantAsync<TContext>(string feature, TContext context, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            return GetVariantAsync<TContext>(feature, context, true, cancellationToken);
        }

        private async ValueTask<Variant> GetVariantAsync<TContext>(string feature, TContext context, bool useContext, CancellationToken cancellationToken)
        {
            FeatureDefinition featureDefinition = await _featureDefinitionProvider
                .GetFeatureDefinitionAsync(feature, cancellationToken)
                .ConfigureAwait(false);

            if (featureDefinition == null)
            {
                throw new FeatureManagementException(
                    FeatureManagementError.MissingFeature,
                    $"The feature declaration for the dynamic feature '{feature}' was not found.");
            }

            if (featureDefinition.Variants == null)
            {
                throw new FeatureManagementException(
                    FeatureManagementError.MissingFeatureVariant,
                    $"No variants are registered for the feature {feature}");
            }

            bool isFeatureEnabled = await IsEnabledAsync(feature, context, true, cancellationToken).ConfigureAwait(false);

            FeatureVariant featureVariant = await GetFeatureVariantAsync(feature, featureDefinition, context, useContext, isFeatureEnabled, cancellationToken).ConfigureAwait(false);

            if (featureVariant == null)
            {
                return null;
            }

            // logic to figure out how to resolve Configuration to one variable betwen value/reference TODO
            Variant returnVariant = new Variant()
            {
                Name = featureVariant.Name,
                ConfigurationValue = featureVariant.ConfigurationValue,
                Configuration = await _variantOptionsResolver.GetOptionsAsync(featureDefinition, featureVariant, cancellationToken).ConfigureAwait(false)
            };

            return returnVariant;
        }

        private async ValueTask<FeatureVariant> GetFeatureVariantAsync<TContext>(string feature, FeatureDefinition featureDefinition, TContext context, bool useContext, bool isFeatureEnabled, CancellationToken cancellationToken)
        {
            FeatureVariant featureVariant;

            const string allocatorName = "Targeting";

            IFeatureVariantAllocatorMetadata allocator = GetFeatureVariantAllocatorMetadata(allocatorName);

            if (allocator == null)
            {
                throw new FeatureManagementException(
                       FeatureManagementError.MissingFeatureVariantAllocator,
                       $"The feature variant allocator for feature '{feature}' was not found.");
            }

            var allocationContext = new FeatureVariantAllocationContext()
            {
                FeatureDefinition = featureDefinition
            };

            //
            // IFeatureVariantAllocator
            if (allocator is IFeatureVariantAllocator featureVariantAllocator)
            {
                featureVariant = await featureVariantAllocator.AllocateVariantAsync(allocationContext, isFeatureEnabled, cancellationToken).ConfigureAwait(false);
            }
            //
            // IContextualFeatureVariantAllocator
            else if (useContext &&
                     TryGetContextualFeatureVariantAllocator(allocatorName, typeof(TContext), out ContextualFeatureVariantAllocatorEvaluator contextualAllocator))
            {
                featureVariant = await contextualAllocator.AllocateVariantAsync(allocationContext, context, isFeatureEnabled, cancellationToken).ConfigureAwait(false);
            }
            //
            // The allocator doesn't implement a feature variant allocator interface capable of performing the evaluation
            else
            {
                throw new FeatureManagementException(
                    FeatureManagementError.InvalidFeatureVariantAllocator,
                    useContext ?
                        $"The feature variant allocator '{allocatorName}' specified for the feature '{feature}' is not capable of evaluating the requested feature with the provided context." :
                        $"The feature variant allocator '{allocatorName}' specified for the feature '{feature}' is not capable of evaluating the requested feature.");
            }

            return featureVariant;
        }

        private IFeatureVariantAllocatorMetadata GetFeatureVariantAllocatorMetadata(string allocatorName)
        {
            const string allocatorSuffix = "allocator";

            IFeatureVariantAllocatorMetadata allocator = _allocatorMetadataCache.GetOrAdd(
                allocatorName,
                (_) => {

                    IEnumerable<IFeatureVariantAllocatorMetadata> matchingAllocators = _variantAllocators.Where(a =>
                    {
                        Type allocatorType = a.GetType();

                        string name = ((AllocatorAliasAttribute)Attribute.GetCustomAttribute(allocatorType, typeof(AllocatorAliasAttribute)))?.Alias;

                        if (name == null)
                        {
                            name = allocatorType.Name;
                        }

                        return NameHelper.IsMatchingReference(
                            reference: allocatorName,
                            metadataName: name,
                            suffix: allocatorSuffix);
                    });

                    if (matchingAllocators.Count() > 1)
                    {
                        throw new FeatureManagementException(FeatureManagementError.AmbiguousFeatureVariantAllocator, $"Multiple feature variant allocators match the configured allocator named '{allocatorName}'.");
                    }

                    return matchingAllocators.FirstOrDefault();
                }
            );

            return allocator;
        }

        private bool TryGetContextualFeatureVariantAllocator(string allocatorName, Type appContextType, out ContextualFeatureVariantAllocatorEvaluator contextualAllocator)
        {
            if (appContextType == null)
            {
                throw new ArgumentNullException(nameof(appContextType));
            }

            contextualAllocator = _contextualFeatureVariantAllocatorCache.GetOrAdd(
                $"{allocatorName}{Environment.NewLine}{appContextType.FullName}",
                (_) => {

                    IFeatureVariantAllocatorMetadata metadata = GetFeatureVariantAllocatorMetadata(allocatorName);

                    return ContextualFeatureVariantAllocatorEvaluator.IsContextualVariantAllocator(metadata, appContextType) ?
                        new ContextualFeatureVariantAllocatorEvaluator(metadata, appContextType) :
                        null;
                }
            );

            return contextualAllocator != null;
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
