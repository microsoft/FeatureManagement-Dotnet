// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement.Targeting;
using Microsoft.FeatureManagement.Telemetry;
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
    /// Used to evaluate the enabled state of a feature and/or get the assigned variant of a feature, if any.
    /// </summary>
    public sealed class FeatureManager : IFeatureManager, IVariantFeatureManager
    {
        private readonly TimeSpan ParametersCacheSlidingExpiration = TimeSpan.FromMinutes(5);
        private readonly TimeSpan ParametersCacheAbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

        private readonly IFeatureDefinitionProvider _featureDefinitionProvider;
        private readonly FeatureManagementOptions _options;
        private readonly ConcurrentDictionary<string, IFeatureFilterMetadata> _filterMetadataCache;
        private readonly ConcurrentDictionary<string, ContextualFeatureFilterEvaluator> _contextualFeatureFilterCache;
        private readonly IEnumerable<IFeatureFilterMetadata> _featureFilters;
        private readonly IEnumerable<ISessionManager> _sessionManagers;
        private readonly IEnumerable<ITelemetryPublisher> _telemetryPublishers;
        private readonly TargetingEvaluationOptions _assignerOptions;

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
            _featureDefinitionProvider = featureDefinitionProvider ?? throw new ArgumentNullException(nameof(featureDefinitionProvider));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _filterMetadataCache = new ConcurrentDictionary<string, IFeatureFilterMetadata>();
            _contextualFeatureFilterCache = new ConcurrentDictionary<string, ContextualFeatureFilterEvaluator>();
            _featureFilters = Enumerable.Empty<IFeatureFilterMetadata>();
            _sessionManagers = Enumerable.Empty<ISessionManager>();
            _telemetryPublishers = Enumerable.Empty<ITelemetryPublisher>();
            _assignerOptions = new TargetingEvaluationOptions();
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
        /// The collection of telemetry publishers.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if it is set to null.</exception>
        public IEnumerable<ITelemetryPublisher> TelemetryPublishers
        {
            get => _telemetryPublishers;

            init
            {
                _telemetryPublishers = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// The configuration reference for feature variants.
        /// </summary>
        public IConfiguration Configuration { get; init; }

        /// <summary>
        /// The targeting context accessor for feature variant allocation.
        /// </summary>
        public ITargetingContextAccessor TargetingContextAccessor { get; init; }

        /// <summary>
        /// Options controlling the targeting behavior for feature variant allocation.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if it is set to null.</exception>
        public TargetingEvaluationOptions AssignerOptions
        {
            get => _assignerOptions;

            init
            {
                _assignerOptions = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        public Task<bool> IsEnabledAsync(string feature)
        {
            return IsEnabledWithVariantsAsync<object>(feature, appContext: null, useAppContext: false, CancellationToken.None).AsTask();
        }

        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="appContext">A context providing information that can be used to evaluate whether a feature should be on or off.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext)
        {
            return IsEnabledWithVariantsAsync(feature, appContext, useAppContext: true, CancellationToken.None).AsTask();
        }

        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        public ValueTask<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken)
        {
            return IsEnabledWithVariantsAsync<object>(feature, appContext: null, useAppContext: false, cancellationToken);
        }

        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="appContext">A context providing information that can be used to evaluate whether a feature should be on or off.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        public ValueTask<bool> IsEnabledAsync<TContext>(string feature, TContext appContext, CancellationToken cancellationToken)
        {
            return IsEnabledWithVariantsAsync(feature, appContext, useAppContext: true, cancellationToken);
        }

        /// <summary>
        /// Retrieves a list of feature names registered in the feature manager.
        /// </summary>
        /// <returns>An enumerator which provides asynchronous iteration over the feature names registered in the feature manager.</returns>
        public IAsyncEnumerable<string> GetFeatureNamesAsync()
        {
            return GetFeatureNamesAsync(CancellationToken.None);
        }

        /// <summary>
        /// Retrieves a list of feature names registered in the feature manager.
        /// </summary>
        /// <returns>An enumerator which provides asynchronous iteration over the feature names registered in the feature manager.</returns>
        public async IAsyncEnumerable<string> GetFeatureNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (FeatureDefinition featureDefinition in _featureDefinitionProvider.GetAllFeatureDefinitionsAsync().ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return featureDefinition.Name;
            }
        }

        /// <summary>
        /// Gets the assigned variant for a specific feature.
        /// </summary>
        /// <param name="feature">The name of the feature to evaluate.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A variant assigned to the user based on the feature's configured allocation.</returns>
        public ValueTask<Variant> GetVariantAsync(string feature, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            return GetVariantAsync(feature, context: null, useContext: false, cancellationToken);
        }

        /// <summary>
        /// Gets the assigned variant for a specific feature.
        /// </summary>
        /// <param name="feature">The name of the feature to evaluate.</param>
        /// <param name="context">An instance of <see cref="TargetingContext"/> used to evaluate which variant the user will be assigned.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A variant assigned to the user based on the feature's configured allocation.</returns>
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

            return GetVariantAsync(feature, context, useContext: true, cancellationToken);
        }

        private async ValueTask<bool> IsEnabledWithVariantsAsync<TContext>(string feature, TContext appContext, bool useAppContext, CancellationToken cancellationToken)
        {
            bool isFeatureEnabled = false;

            FeatureDefinition featureDefinition = await GetFeatureDefinition(feature).ConfigureAwait(false);

            VariantDefinition variantDefinition = null;

            if (featureDefinition != null)
            {
                isFeatureEnabled = await IsEnabledAsync(featureDefinition, appContext, useAppContext, cancellationToken).ConfigureAwait(false);

                if (featureDefinition.Variants != null && featureDefinition.Variants.Any() && featureDefinition.Allocation != null)
                {
                    if (!isFeatureEnabled)
                    {
                        variantDefinition = featureDefinition.Variants.FirstOrDefault((variant) => variant.Name == featureDefinition.Allocation.DefaultWhenDisabled);
                    }
                    else
                    {
                        TargetingContext targetingContext;

                        if (useAppContext)
                        {
                            targetingContext = appContext as TargetingContext;
                        }
                        else
                        {
                            targetingContext = await ResolveTargetingContextAsync(cancellationToken).ConfigureAwait(false);
                        }

                        variantDefinition = await GetAssignedVariantAsync(
                            featureDefinition,
                            targetingContext,
                            cancellationToken)
                            .ConfigureAwait(false);
                    }

                    if (variantDefinition != null && featureDefinition.Status != FeatureStatus.Disabled)
                    {
                        if (variantDefinition.StatusOverride == StatusOverride.Enabled)
                        {
                            isFeatureEnabled = true;
                        }
                        else if (variantDefinition.StatusOverride == StatusOverride.Disabled)
                        {
                            isFeatureEnabled = false;
                        }
                    }
                }
            }

            foreach (ISessionManager sessionManager in _sessionManagers)
            {
                await sessionManager.SetAsync(feature, isFeatureEnabled).ConfigureAwait(false);
            }

            if (featureDefinition.TelemetryEnabled)
            {
                PublishTelemetry(new EvaluationEvent
                {
                    FeatureDefinition = featureDefinition,
                    IsEnabled = isFeatureEnabled,
                    Variant = variantDefinition != null ? GetVariantFromVariantDefinition(variantDefinition) : null
                }, cancellationToken);
            }

            return isFeatureEnabled;
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

                        string errorMessage = $"The feature filter '{featureFilterConfiguration.Name}' specified for feature '{featureDefinition.Name}' was not found.";

                        if (!_options.IgnoreMissingFeatureFilters)
                        {
                            throw new FeatureManagementException(FeatureManagementError.MissingFeatureFilter, errorMessage);
                        }

                        Logger?.LogWarning(errorMessage);

                        continue;
                    }

                    var context = new FeatureFilterEvaluationContext()
                    {
                        FeatureName = featureDefinition.Name,
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

            return enabled;
        }

        private async ValueTask<Variant> GetVariantAsync(string feature, TargetingContext context, bool useContext, CancellationToken cancellationToken)
        {
            FeatureDefinition featureDefinition = await GetFeatureDefinition(feature).ConfigureAwait(false);

            if (featureDefinition == null || featureDefinition.Allocation == null || (!featureDefinition.Variants?.Any() ?? false))
            {
                return null;
            }

            VariantDefinition variantDefinition = null;

            bool isFeatureEnabled = await IsEnabledAsync(featureDefinition, context, useContext, cancellationToken).ConfigureAwait(false);

            if (!isFeatureEnabled)
            {
                variantDefinition = featureDefinition.Variants.FirstOrDefault((variant) => variant.Name == featureDefinition.Allocation.DefaultWhenDisabled);
            }
            else
            {
                if (!useContext)
                {
                    context = await ResolveTargetingContextAsync(cancellationToken).ConfigureAwait(false);
                }

                variantDefinition = await GetAssignedVariantAsync(featureDefinition, context, cancellationToken).ConfigureAwait(false);
            }

            Variant variant = variantDefinition != null ? GetVariantFromVariantDefinition(variantDefinition) : null;

            if (featureDefinition.TelemetryEnabled)
            {
                PublishTelemetry(new EvaluationEvent
                {
                    FeatureDefinition = featureDefinition,
                    IsEnabled = isFeatureEnabled,
                    Variant = variant
                }, cancellationToken);
            }

            return variant;
        }

        private async ValueTask<FeatureDefinition> GetFeatureDefinition(string feature)
        {
            FeatureDefinition featureDefinition = await _featureDefinitionProvider
                .GetFeatureDefinitionAsync(feature)
                .ConfigureAwait(false);

            if (featureDefinition == null)
            {
                string errorMessage = $"The feature definition for the feature '{feature}' was not found.";

                if (!_options.IgnoreMissingFeatures)
                {
                    throw new FeatureManagementException(FeatureManagementError.MissingFeature, errorMessage);
                }

                Logger?.LogDebug(errorMessage);
            }

            return featureDefinition;
        }

        private async ValueTask<TargetingContext> ResolveTargetingContextAsync(CancellationToken cancellationToken)
        {
            if (TargetingContextAccessor == null)
            {
                Logger?.LogWarning($"No instance of {nameof(ITargetingContextAccessor)} is available for variant assignment.");

                return null;
            }

            //
            // Acquire targeting context via accessor
            TargetingContext context = await TargetingContextAccessor.GetContextAsync().ConfigureAwait(false);

            //
            // Ensure targeting can be performed
            if (context == null)
            {
                Logger?.LogWarning($"No instance of {nameof(TargetingContext)} could be found using {nameof(ITargetingContextAccessor)} for variant assignment.");
            }

            return context;
        }

        private async ValueTask<VariantDefinition> GetAssignedVariantAsync(FeatureDefinition featureDefinition, TargetingContext context, CancellationToken cancellationToken)
        {
            VariantDefinition variantDefinition = null;

            if (context != null)
            {
                variantDefinition = await AssignVariantAsync(featureDefinition, context, cancellationToken).ConfigureAwait(false);
            }

            if (variantDefinition == null)
            {
                variantDefinition = featureDefinition.Variants.FirstOrDefault((variant) => variant.Name == featureDefinition.Allocation.DefaultWhenEnabled);
            }

            return variantDefinition;
        }

        private ValueTask<VariantDefinition> AssignVariantAsync(FeatureDefinition featureDefinition, TargetingContext targetingContext, CancellationToken cancellationToken)
        {
            VariantDefinition variant = null;

            if (featureDefinition.Allocation.User != null)
            {
                foreach (UserAllocation user in featureDefinition.Allocation.User)
                {
                    if (TargetingEvaluator.IsTargeted(targetingContext.UserId, user.Users, _assignerOptions.IgnoreCase))
                    {
                        if (string.IsNullOrEmpty(user.Variant))
                        {
                            Logger?.LogWarning($"Missing variant name for user allocation in feature {featureDefinition.Name}");

                            return new ValueTask<VariantDefinition>((VariantDefinition)null);
                        }

                        Debug.Assert(featureDefinition.Variants != null);

                        return new ValueTask<VariantDefinition>(
                            featureDefinition
                                .Variants
                                .FirstOrDefault((variant) => variant.Name == user.Variant));
                    }
                }
            }

            if (featureDefinition.Allocation.Group != null)
            {
                foreach (GroupAllocation group in featureDefinition.Allocation.Group)
                {
                    if (TargetingEvaluator.IsTargeted(targetingContext.Groups, group.Groups, _assignerOptions.IgnoreCase))
                    {
                        if (string.IsNullOrEmpty(group.Variant))
                        {
                            Logger?.LogWarning($"Missing variant name for group allocation in feature {featureDefinition.Name}");

                            return new ValueTask<VariantDefinition>((VariantDefinition)null);
                        }

                        Debug.Assert(featureDefinition.Variants != null);

                        return new ValueTask<VariantDefinition>(
                            featureDefinition
                                .Variants
                                .FirstOrDefault((variant) => variant.Name == group.Variant));
                    }
                }
            }

            if (featureDefinition.Allocation.Percentile != null)
            {
                foreach (PercentileAllocation percentile in featureDefinition.Allocation.Percentile)
                {
                    if (TargetingEvaluator.IsTargeted(
                        targetingContext,
                        percentile.From,
                        percentile.To,
                        _assignerOptions.IgnoreCase,
                        featureDefinition.Allocation.Seed ?? $"allocation\n{featureDefinition.Name}"))
                    {
                        if (string.IsNullOrEmpty(percentile.Variant))
                        {
                            Logger?.LogWarning($"Missing variant name for percentile allocation in feature {featureDefinition.Name}");

                            return new ValueTask<VariantDefinition>((VariantDefinition)null);
                        }

                        Debug.Assert(featureDefinition.Variants != null);

                        return new ValueTask<VariantDefinition>(
                            featureDefinition
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

            if (!(_featureDefinitionProvider is IFeatureDefinitionProviderCacheable) || Cache == null)
            {
                context.Settings = binder.BindParameters(context.Parameters);

                return;
            }

            object settings;

            ConfigurationCacheItem cacheItem;

            string cacheKey = $"Microsoft.FeatureManagement{Environment.NewLine}{context.FeatureName}{Environment.NewLine}{filterIndex}";

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

        private async void PublishTelemetry(EvaluationEvent evaluationEvent, CancellationToken cancellationToken)
        {
            if (!_telemetryPublishers.Any())
            {
                Logger?.LogWarning("The feature declaration enabled telemetry but no telemetry publisher was registered.");
            }
            else
            {
                foreach (ITelemetryPublisher telemetryPublisher in _telemetryPublishers)
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
                    Logger?.LogWarning($"Cannot use {nameof(variantDefinition.ConfigurationReference)} as no instance of {nameof(IConfiguration)} is present.");

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
