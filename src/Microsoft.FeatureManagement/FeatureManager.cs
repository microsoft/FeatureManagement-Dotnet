// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Telemetry;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement.Targeting;
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
    class FeatureManager : IFeatureManager, IDisposable, IVariantFeatureManager
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
            IOptions<TargetingEvaluationOptions> assignerOptions)
        {
            _featureDefinitionProvider = featureDefinitionProvider;
            _featureFilters = featureFilters ?? throw new ArgumentNullException(nameof(featureFilters));
            _sessionManagers = sessionManagers ?? throw new ArgumentNullException(nameof(sessionManagers));
            _logger = loggerFactory.CreateLogger<FeatureManager>();
            _assignerOptions = assignerOptions?.Value ?? throw new ArgumentNullException(nameof(assignerOptions));
            _filterMetadataCache = new ConcurrentDictionary<string, IFeatureFilterMetadata>();
            _contextualFeatureFilterCache = new ConcurrentDictionary<string, ContextualFeatureFilterEvaluator>();
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _parametersCache = new MemoryCache(new MemoryCacheOptions());
        }

        public IEnumerable<ITelemetryPublisher> TelemetryPublishers { get; init; }

        public IConfiguration Configuration { get; init; }

        public ITargetingContextAccessor TargetingContextAccessor { get; init; }

        public Task<bool> IsEnabledAsync(string feature)
        {
            return IsEnabledEvaluation<object>(feature, appContext: null, useAppContext: false, CancellationToken.None).AsTask();
        }

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext)
        {
            return IsEnabledEvaluation(feature, appContext, useAppContext: true, CancellationToken.None).AsTask();
        }

        public ValueTask<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken)
        {
            return IsEnabledEvaluation<object>(feature, appContext: null, useAppContext: false, cancellationToken);
        }

        public ValueTask<bool> IsEnabledAsync<TContext>(string feature, TContext appContext, CancellationToken cancellationToken)
        {
            return IsEnabledEvaluation(feature, appContext, useAppContext: true, cancellationToken);
        }

        private async ValueTask<bool> IsEnabledEvaluation<TContext>(string feature, TContext appContext, bool useAppContext, CancellationToken cancellationToken)
        {
            EvaluationEvent evaluationEvent = new EvaluationEvent
            {
                FeatureDefinition = await GetFeatureDefinition(feature).ConfigureAwait(false)
            };

            await EvaluateFeature(evaluationEvent, appContext, useAppContext, cancellationToken);

            return evaluationEvent.IsEnabled;
        }

        public ValueTask<Variant> GetVariantAsync(string feature, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            return GetVariantEvaluation(feature, context: null, useContext: false, cancellationToken);
        }

        public ValueTask<Variant> GetVariantAsync(string feature, TargetingContext context, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return GetVariantEvaluation(feature, context, useContext: true, cancellationToken);
        }

        private async ValueTask<Variant> GetVariantEvaluation(string feature, TargetingContext context, bool useContext, CancellationToken cancellationToken)
        {
            EvaluationEvent evaluationEvent = new EvaluationEvent
            {
                FeatureDefinition = await GetFeatureDefinition(feature).ConfigureAwait(false)
            };

            await EvaluateFeature(evaluationEvent, context, useContext, cancellationToken);

            return evaluationEvent.Variant;
        }

        private async Task<EvaluationEvent> EvaluateFeature<TContext>(EvaluationEvent evaluationEvent, TContext context, bool useContext, CancellationToken cancellationToken)
        {
            if (evaluationEvent.FeatureDefinition != null)
            {
                //
                // Determine IsEnabled
                evaluationEvent.IsEnabled = await IsEnabledAsync(evaluationEvent.FeatureDefinition, context, useContext, cancellationToken).ConfigureAwait(false);

                //
                // Determine Variant
                VariantDefinition variantDefinition;

                if (evaluationEvent.FeatureDefinition.Allocation == null || (!evaluationEvent.FeatureDefinition.Variants?.Any() ?? false))
                {
                    variantDefinition = null;

                    evaluationEvent.VariantReason = "No Allocation or Variants";
                }
                else
                {
                    if (!evaluationEvent.IsEnabled)
                    {
                        variantDefinition = evaluationEvent.FeatureDefinition.Variants.FirstOrDefault((variant) => variant.Name == evaluationEvent.FeatureDefinition.Allocation.DefaultWhenDisabled);

                        evaluationEvent.VariantReason = "Disabled Default";
                    }
                    else
                    {
                        TargetingContext targetingContext;

                        if (useContext)
                        {
                            targetingContext = context as TargetingContext;
                        }
                        else
                        {
                            targetingContext = await ResolveTargetingContextAsync(cancellationToken).ConfigureAwait(false);
                        }

                        variantDefinition = await GetAssignedVariantAsync(
                            evaluationEvent,
                            targetingContext,
                            cancellationToken)
                            .ConfigureAwait(false);
                    }

                    evaluationEvent.Variant = variantDefinition != null ? GetVariantFromVariantDefinition(variantDefinition) : null;

                    //
                    // Override IsEnabled if variant has an override
                    if (variantDefinition != null && evaluationEvent.FeatureDefinition.Status != FeatureStatus.Disabled)
                    {
                        if (variantDefinition.StatusOverride == StatusOverride.Enabled)
                        {
                            evaluationEvent.IsEnabled = true;
                        }
                        else if (variantDefinition.StatusOverride == StatusOverride.Disabled)
                        {
                            evaluationEvent.IsEnabled = false;
                        }
                    }
                }
            }

            foreach (ISessionManager sessionManager in _sessionManagers)
            {
                await sessionManager.SetAsync(evaluationEvent.FeatureDefinition.Name, evaluationEvent.IsEnabled).ConfigureAwait(false);
            }

            if (evaluationEvent.FeatureDefinition.TelemetryEnabled)
            {
                PublishTelemetry(evaluationEvent, cancellationToken);
            }

            return evaluationEvent;
        }

        public IAsyncEnumerable<string> GetFeatureNamesAsync()
        {
            return GetFeatureNamesAsync(CancellationToken.None);
        }

        public async IAsyncEnumerable<string> GetFeatureNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (FeatureDefinition featureDefinition in _featureDefinitionProvider.GetAllFeatureDefinitionsAsync().ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return featureDefinition.Name;
            }
        }

        public void Dispose()
        {
            _parametersCache.Dispose();
        }

        private async Task<bool> IsEnabledAsync<TContext>(FeatureDefinition featureDefinition, TContext appContext, bool useAppContext, CancellationToken cancellationToken)
        {
            Debug.Assert(featureDefinition != null);

            foreach (ISessionManager sessionManager in _sessionManagers)
            {
                bool? readSessionResult = await sessionManager.GetAsync(featureDefinition.Name).ConfigureAwait(false);

                if (readSessionResult.HasValue)
                {
                    return readSessionResult.Value;
                }
            }

            bool enabled;

            //
            // Treat an empty or status disabled feature as disabled
            if (featureDefinition.EnabledFor == null || 
                !featureDefinition.EnabledFor.Any() || 
                featureDefinition.Status == FeatureStatus.Disabled)
            {
                enabled = false;
            }
            else
            {
                //
                // Ensure no conflicts in the feature definition
                if (featureDefinition.RequirementType == RequirementType.All && _options.IgnoreMissingFeatureFilters)
                {
                    throw new FeatureManagementException(
                        FeatureManagementError.Conflict,
                        $"The 'IgnoreMissingFeatureFilters' flag cannot be used in combination with a feature of requirement type 'All'.");
                }

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
                        string errorMessage = $"The feature filter '{featureFilterConfiguration.Name}' specified for feature '{featureDefinition.Name}' was not found.";

                        if (!_options.IgnoreMissingFeatureFilters)
                        {
                            throw new FeatureManagementException(FeatureManagementError.MissingFeatureFilter, errorMessage);
                        }

                        _logger.LogWarning(errorMessage);

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

            return enabled;
        }

        private async ValueTask<FeatureDefinition> GetFeatureDefinition(string feature)
        {
            FeatureDefinition featureDefinition = await _featureDefinitionProvider
                .GetFeatureDefinitionAsync(feature)
                .ConfigureAwait(false);

            if (featureDefinition == null)
            {
                string errorMessage = $"The feature declaration for the feature '{feature}' was not found.";

                if (!_options.IgnoreMissingFeatures)
                {
                    throw new FeatureManagementException(FeatureManagementError.MissingFeature, errorMessage);
                }

                _logger.LogWarning(errorMessage);
            }

            return featureDefinition;
        }

        private async ValueTask<TargetingContext> ResolveTargetingContextAsync(CancellationToken cancellationToken)
        {
            if (TargetingContextAccessor == null)
            {
                _logger.LogWarning($"No instance of {nameof(ITargetingContextAccessor)} is available for variant assignment.");

                return null;
            }

            //
            // Acquire targeting context via accessor
            TargetingContext context = await TargetingContextAccessor.GetContextAsync().ConfigureAwait(false);

            //
            // Ensure targeting can be performed
            if (context == null)
            {
                _logger.LogWarning($"No instance of {nameof(TargetingContext)} could be found using {nameof(ITargetingContextAccessor)} for variant assignment.");
            }

            return context;
        }

        private async ValueTask<VariantDefinition> GetAssignedVariantAsync(EvaluationEvent evaluationEvent, TargetingContext context, CancellationToken cancellationToken)
        {
            VariantDefinition variantDefinition = null;

            if (context != null)
            {
                variantDefinition = await AssignVariantAsync(evaluationEvent, context, cancellationToken).ConfigureAwait(false);
            }

            if (variantDefinition == null)
            {
                variantDefinition = evaluationEvent.FeatureDefinition.Variants.FirstOrDefault((variant) => variant.Name == evaluationEvent.FeatureDefinition.Allocation.DefaultWhenEnabled);

                evaluationEvent.VariantReason = "Enabled Default";
            }

            return variantDefinition;
        }

        private ValueTask<VariantDefinition> AssignVariantAsync(EvaluationEvent evaluationEvent, TargetingContext targetingContext, CancellationToken cancellationToken)
        {
            VariantDefinition variant = null;

            if (evaluationEvent.FeatureDefinition.Allocation.User != null)
            {
                foreach (UserAllocation user in evaluationEvent.FeatureDefinition.Allocation.User)
                {
                    if (TargetingEvaluator.IsTargeted(targetingContext.UserId, user.Users, _assignerOptions.IgnoreCase))
                    {
                        if (string.IsNullOrEmpty(user.Variant))
                        {
                            _logger.LogWarning($"Missing variant name for user allocation in feature {evaluationEvent.FeatureDefinition.Name}");

                            return new ValueTask<VariantDefinition>((VariantDefinition)null);
                        }

                        Debug.Assert(evaluationEvent.FeatureDefinition.Variants != null);

                        evaluationEvent.VariantReason = "User Allocated";

                        return new ValueTask<VariantDefinition>(
                            evaluationEvent.FeatureDefinition
                                .Variants
                                .FirstOrDefault((variant) => variant.Name == user.Variant));
                    }
                }
            }

            if (evaluationEvent.FeatureDefinition.Allocation.Group != null)
            {
                foreach (GroupAllocation group in evaluationEvent.FeatureDefinition.Allocation.Group)
                {
                    if (TargetingEvaluator.IsTargeted(targetingContext.Groups, group.Groups, _assignerOptions.IgnoreCase))
                    {
                        if (string.IsNullOrEmpty(group.Variant))
                        {
                            _logger.LogWarning($"Missing variant name for group allocation in feature {evaluationEvent.FeatureDefinition.Name}");

                            return new ValueTask<VariantDefinition>((VariantDefinition)null);
                        }

                        Debug.Assert(evaluationEvent.FeatureDefinition.Variants != null);

                        evaluationEvent.VariantReason = "Group Allocated";

                        return new ValueTask<VariantDefinition>(
                            evaluationEvent.FeatureDefinition
                                .Variants
                                .FirstOrDefault((variant) => variant.Name == group.Variant));
                    }
                }
            }

            if (evaluationEvent.FeatureDefinition.Allocation.Percentile != null)
            {
                foreach (PercentileAllocation percentile in evaluationEvent.FeatureDefinition.Allocation.Percentile)
                {
                    if (TargetingEvaluator.IsTargeted(
                        targetingContext,
                        percentile.From,
                        percentile.To,
                        _assignerOptions.IgnoreCase,
                        evaluationEvent.FeatureDefinition.Allocation.Seed ?? $"allocation\n{evaluationEvent.FeatureDefinition.Name}"))
                    {
                        if (string.IsNullOrEmpty(percentile.Variant))
                        {
                            _logger.LogWarning($"Missing variant name for percentile allocation in feature {evaluationEvent.FeatureDefinition.Name}");

                            return new ValueTask<VariantDefinition>((VariantDefinition)null);
                        }

                        Debug.Assert(evaluationEvent.FeatureDefinition.Variants != null);

                        evaluationEvent.VariantReason = "Percentile Allocated";

                        return new ValueTask<VariantDefinition>(
                            evaluationEvent.FeatureDefinition
                                .Variants
                                .FirstOrDefault((variant) => variant.Name == percentile.Variant));
                    }
                }
            }

            return new ValueTask<VariantDefinition>(variant);
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

        private async void PublishTelemetry(EvaluationEvent evaluationEvent, CancellationToken cancellationToken)
        {
            if (TelemetryPublishers == null || !TelemetryPublishers.Any())
            {
                _logger.LogWarning("The feature declaration enabled telemetry but no telemetry publisher was registered.");
            }
            else
            {
                foreach (ITelemetryPublisher telemetryPublisher in TelemetryPublishers)
                {
                    await telemetryPublisher.PublishEvent(
                        evaluationEvent,
                        cancellationToken);
                }
            }
        }

        private Variant GetVariantFromVariantDefinition(VariantDefinition variantDefinition)
        {
            IConfigurationSection variantConfiguration = null;

            if (variantDefinition.ConfigurationValue.Exists())
            {
                variantConfiguration = variantDefinition.ConfigurationValue;
            }
            else if (!string.IsNullOrEmpty(variantDefinition.ConfigurationReference))
            {
                if (Configuration == null)
                {
                    _logger.LogWarning($"Cannot use {nameof(variantDefinition.ConfigurationReference)} as no instance of {nameof(IConfiguration)} is present.");

                    return null;
                }
                else
                {
                    variantConfiguration = Configuration.GetSection(variantDefinition.ConfigurationReference);
                }
            }

            return new Variant()
            {
                Name = variantDefinition.Name,
                Configuration = variantConfiguration
            };
        }
    }
}
