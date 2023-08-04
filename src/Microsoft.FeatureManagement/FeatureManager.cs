// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement.Targeting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ITargetingContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, IFeatureFilterMetadata> _filterMetadataCache;
        private readonly ConcurrentDictionary<string, ContextualFeatureFilterEvaluator> _contextualFeatureFilterCache;
        private readonly FeatureManagementOptions _options;
        private readonly TargetingEvaluationOptions _assignerOptions;
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
            IOptions<FeatureManagementOptions> options,
            IOptions<TargetingEvaluationOptions> assignerOptions,
            IConfiguration configuration = null,
            ITargetingContextAccessor contextAccessor = null)
        {
            _featureDefinitionProvider = featureDefinitionProvider;
            _featureFilters = featureFilters ?? throw new ArgumentNullException(nameof(featureFilters));
            _sessionManagers = sessionManagers ?? throw new ArgumentNullException(nameof(sessionManagers));
            _logger = loggerFactory.CreateLogger<FeatureManager>();
            _assignerOptions = assignerOptions?.Value ?? throw new ArgumentNullException(nameof(assignerOptions));
            _contextAccessor = contextAccessor;
            _configuration = configuration;
            _filterMetadataCache = new ConcurrentDictionary<string, IFeatureFilterMetadata>();
            _contextualFeatureFilterCache = new ConcurrentDictionary<string, ContextualFeatureFilterEvaluator>();
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _parametersCache = new MemoryCache(new MemoryCacheOptions());
        }

        public Task<bool> IsEnabledAsync(string feature)
        {
            return IsEnabledAsync<object>(feature, null, false, false);
        }

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext)
        {
            return IsEnabledAsync(feature, appContext, true, false);
        }

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext, bool ignoreVariant)
        {
            return IsEnabledAsync(feature, appContext, true, ignoreVariant);
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

        private async Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext, bool useAppContext, bool ignoreVariant)
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
                        // Handle AlwaysOn and On filters
                        if (string.Equals(featureFilterConfiguration.Name, "AlwaysOn", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(featureFilterConfiguration.Name, "On", StringComparison.OrdinalIgnoreCase))
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

                            if (await featureFilter.EvaluateAsync(context).ConfigureAwait(false) == targetEvaluation)
                            {
                                enabled = targetEvaluation;

                                break;
                            }
                        }
                    }
                }

                if (!ignoreVariant && (featureDefinition.Variants?.Any() ?? false) && featureDefinition.Allocation != null && featureDefinition.Status != Status.Disabled)
                {
                    FeatureVariant featureVariant = await GetFeatureVariantAsync(featureDefinition, appContext as TargetingContext, useAppContext, enabled, CancellationToken.None);

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
                await sessionManager.SetAsync(feature, enabled).ConfigureAwait(false);
            }

            return enabled;
        }

        public ValueTask<Variant> GetVariantAsync(string feature, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            return GetVariantAsync(feature, null, false, cancellationToken);
        }

        public ValueTask<Variant> GetVariantAsync(string feature, TargetingContext context, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            return GetVariantAsync(feature, context, true, cancellationToken);
        }

        private async ValueTask<Variant> GetVariantAsync(string feature, TargetingContext context, bool useContext, CancellationToken cancellationToken)
        {
            FeatureDefinition featureDefinition = await _featureDefinitionProvider
                .GetFeatureDefinitionAsync(feature)
                .ConfigureAwait(false);

            if (featureDefinition == null)
            {
                throw new FeatureManagementException(
                    FeatureManagementError.MissingFeature,
                    $"The feature declaration for the feature '{feature}' was not found.");
            }

            if (featureDefinition.Allocation == null)
            {
                throw new FeatureManagementException(
                    FeatureManagementError.MissingAllocation,
                    $"No allocation is defined for the feature {featureDefinition.Name}");
            }

            if (!featureDefinition.Variants?.Any() ?? false)
            {
                throw new FeatureManagementException(
                    FeatureManagementError.MissingFeatureVariant,
                    $"No variants are registered for the feature {feature}");
            }

            FeatureVariant featureVariant;

            bool isFeatureEnabled = await IsEnabledAsync(feature, context, true).ConfigureAwait(false);

            featureVariant = await GetFeatureVariantAsync(featureDefinition, context, useContext, isFeatureEnabled, cancellationToken).ConfigureAwait(false);

            if (featureVariant == null)
            {
                return null;
            }

            IConfigurationSection variantConfiguration = null;

            bool configValueSet = !string.IsNullOrEmpty(featureVariant.ConfigurationValue);
            bool configReferenceValueSet = !string.IsNullOrEmpty(featureVariant.ConfigurationReference);

            if (configValueSet && configReferenceValueSet)
            {
                throw new FeatureManagementException(
                    FeatureManagementError.InvalidVariantConfiguration,
                    $"Both ConfigurationValue and ConfigurationReference are specified for the variant {featureVariant.Name} in feature {feature}");
            }
            else if (configReferenceValueSet)
            {
                if (_configuration == null)
                {
                    throw new FeatureManagementException(
                        FeatureManagementError.InvalidVariantConfiguration,
                        $"Cannot use {nameof(featureVariant.ConfigurationReference)} if no instance of {nameof(IConfiguration)} is present.");
                }

                variantConfiguration = _configuration.GetSection(featureVariant.ConfigurationReference);
            }
            else if (configValueSet)
            {
                VariantConfigurationSection section = new VariantConfigurationSection(featureVariant.Name, "", featureVariant.ConfigurationValue);
                variantConfiguration = section;
            }

            Variant returnVariant = new Variant()
            {
                Name = featureVariant.Name,
                Configuration = variantConfiguration
            };

            return returnVariant;
        }

        private async ValueTask<FeatureVariant> GetFeatureVariantAsync(FeatureDefinition featureDefinition, TargetingContext context, bool useContext, bool isFeatureEnabled, CancellationToken cancellationToken)
        {
            if (!isFeatureEnabled)
            {
                return ResolveDefaultFeatureVariant(featureDefinition, isFeatureEnabled);
            }

            FeatureVariant featureVariant = null;

            if (!useContext) 
            {
                if (_contextAccessor == null)
                {
                    _logger.LogWarning($"No instance of {nameof(ITargetingContextAccessor)} is available for targeting evaluation. Using default variants.");
                }
                else
                {
                    //
                    // Acquire targeting context via accessor
                    context = await _contextAccessor.GetContextAsync().ConfigureAwait(false);

                    //
                    // Ensure targeting can be performed
                    if (context == null)
                    {
                        _logger.LogWarning("No targeting context available for targeting evaluation.");

                        return null;
                    }
                }
            }

            if (context != null)
            {
                featureVariant = await AssignVariantAsync(featureDefinition, context, cancellationToken).ConfigureAwait(false);
            }

            if (featureVariant == null)
            {
                featureVariant = ResolveDefaultFeatureVariant(featureDefinition, isFeatureEnabled);
            }

            return featureVariant;
        }

        private ValueTask<FeatureVariant> AssignVariantAsync(FeatureDefinition featureDefinition, TargetingContext targetingContext, CancellationToken cancellationToken)
        {
            FeatureVariant variant = null;

            if (featureDefinition.Allocation.User != null)
            {
                foreach (User user in featureDefinition.Allocation.User)
                {
                    if (TargetingEvaluator.IsTargeted(targetingContext, user.Users, _assignerOptions.IgnoreCase))
                    {
                        variant = featureDefinition.Variants.FirstOrDefault((variant) => variant.Name.Equals(user.Variant));

                        if (!string.IsNullOrEmpty(variant.Name))
                        {
                            return new ValueTask<FeatureVariant>(variant);
                        }
                    }
                }
            }

            if (featureDefinition.Allocation.Group != null)
            {
                foreach (Group group in featureDefinition.Allocation.Group)
                {
                    if (TargetingEvaluator.IsGroupTargeted(targetingContext, group.Groups, _assignerOptions.IgnoreCase))
                    {
                        variant = featureDefinition.Variants.FirstOrDefault((variant) => variant.Name.Equals(group.Variant));

                        if (!string.IsNullOrEmpty(variant.Name))
                        {
                            return new ValueTask<FeatureVariant>(variant);
                        }
                    }
                }
            }

            if (featureDefinition.Allocation.Percentile != null)
            {
                foreach (Percentile percentile in featureDefinition.Allocation.Percentile)
                {
                    if (TargetingEvaluator.IsTargeted(targetingContext, percentile.From, percentile.To, featureDefinition.Allocation.Seed, _assignerOptions.IgnoreCase, featureDefinition.Name))
                    {
                        variant = featureDefinition.Variants.FirstOrDefault((variant) => variant.Name.Equals(percentile.Variant));

                        if (!string.IsNullOrEmpty(variant.Name))
                        {
                            return new ValueTask<FeatureVariant>(variant);
                        }
                    }
                }
            }

            return new ValueTask<FeatureVariant>(variant);
        }

        private FeatureVariant ResolveDefaultFeatureVariant(FeatureDefinition featureDefinition, bool isFeatureEnabled)
        {
            string defaultVariantPath = isFeatureEnabled ? featureDefinition.Allocation.DefaultWhenEnabled : featureDefinition.Allocation.DefaultWhenDisabled;

            if (!string.IsNullOrEmpty(defaultVariantPath))
            {
                FeatureVariant defaultVariant = featureDefinition.Variants.FirstOrDefault((variant) => variant.Name.Equals(defaultVariantPath));

                if (!string.IsNullOrEmpty(defaultVariant.Name))
                {
                    return defaultVariant;
                }
            }
            
            return null;
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
