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
        private const string FeatureDefinitionNotFoundError = "The feature definition for the feature '{0}' was not found.";
        private const string FeatureFilterNotFoundError = "The feature filter '{0}' specified for feature '{1}' was not found.";

        private const string AlwaysOnFilterName = "AlwaysOn";
        private const string OnFilterName = "On";
        private const string FilterSuffix = "filter";

        private readonly TimeSpan ParametersCacheSlidingExpiration = TimeSpan.FromMinutes(5);
        private readonly TimeSpan ParametersCacheAbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

        private readonly IFeatureDefinitionProvider _featureDefinitionProvider;
        private readonly ConcurrentDictionary<string, IFeatureFilterMetadata> _filterMetadataCache;
        private readonly ConcurrentDictionary<string, ContextualFeatureFilterEvaluator> _contextualFeatureFilterCache;
        private readonly FeatureManagementOptions _options;

        private IEnumerable<IFeatureFilterMetadata> _featureFilters;
        private IEnumerable<ISessionManager> _sessionManagers;
        private TargetingEvaluationOptions _assignerOptions;

        /// <summary>
        /// The activity source for feature management.
        /// </summary>
        private static readonly ActivitySource ActivitySource = new ActivitySource("Microsoft.FeatureManagement", "1.0.0");

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
        public FeatureManager(
            IFeatureDefinitionProvider featureDefinitionProvider,
            FeatureManagementOptions options = null)
        {
            _featureDefinitionProvider = featureDefinitionProvider ?? throw new ArgumentNullException(nameof(featureDefinitionProvider));
            _options = options ?? new FeatureManagementOptions();
            _filterMetadataCache = new ConcurrentDictionary<string, IFeatureFilterMetadata>();
            _contextualFeatureFilterCache = new ConcurrentDictionary<string, ContextualFeatureFilterEvaluator>();
            _assignerOptions = new TargetingEvaluationOptions();
            _featureFilters = Enumerable.Empty<IFeatureFilterMetadata>();
        }

        /// <summary>
        /// The collection of feature filter metadata.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if it is set to null.</exception>
        public IEnumerable<IFeatureFilterMetadata> FeatureFilters
        {
            get => _featureFilters;

            set
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
            get
            {
                if (_sessionManagers == null)
                {
                    _sessionManagers = Enumerable.Empty<ISessionManager>();
                }

                return _sessionManagers;
            }

            set
            {
                _sessionManagers = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// The application memory cache to store feature filter settings.
        /// </summary>
        public IMemoryCache Cache { get; set; }

        /// <summary>
        /// The logger for the feature manager.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// The targeting context accessor for feature variant allocation.
        /// </summary>
        public ITargetingContextAccessor TargetingContextAccessor { get; set; }

        /// <summary>
        /// Options controlling the targeting behavior for feature variant allocation.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if it is set to null.</exception>
        public TargetingEvaluationOptions AssignerOptions
        {
            get => _assignerOptions;

            set
            {
                _assignerOptions = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        public async Task<bool> IsEnabledAsync(string feature)
        {
            return (await EvaluateFeature<object>(feature, context: null, useContext: false, CancellationToken.None).ConfigureAwait(false)).Enabled;
        }

        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="appContext">A context that provides information to evaluate whether a feature should be on or off.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        public async Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext)
        {
            return (await EvaluateFeature(feature, context: appContext, useContext: true, CancellationToken.None).ConfigureAwait(false)).Enabled;
        }

        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        public async ValueTask<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken = default)
        {
            return (await EvaluateFeature<object>(feature, context: null, useContext: false, cancellationToken).ConfigureAwait(false)).Enabled;
        }

        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="appContext">A context that provides information to evaluate whether a feature should be on or off.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        public async ValueTask<bool> IsEnabledAsync<TContext>(string feature, TContext appContext, CancellationToken cancellationToken = default)
        {
            return (await EvaluateFeature(feature, context: appContext, useContext: true, cancellationToken).ConfigureAwait(false)).Enabled;
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
        public async IAsyncEnumerable<string> GetFeatureNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var featureNamesReturned = new HashSet<string>();
            await foreach (FeatureDefinition featureDefinition in _featureDefinitionProvider.GetAllFeatureDefinitionsAsync().ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!featureNamesReturned.Contains(featureDefinition.Name))
                {
                    yield return featureDefinition.Name;

                    featureNamesReturned.Add(featureDefinition.Name);
                }
            }
        }

        /// <summary>
        /// Gets the assigned variant for a specific feature.
        /// </summary>
        /// <param name="feature">The name of the feature to evaluate.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A variant assigned to the user based on the feature's configured allocation.</returns>
        public async ValueTask<Variant> GetVariantAsync(string feature, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            return (await EvaluateFeature<object>(feature, context: null, useContext: false, cancellationToken).ConfigureAwait(false)).Variant;
        }

        /// <summary>
        /// Gets the assigned variant for a specific feature.
        /// </summary>
        /// <param name="feature">The name of the feature to evaluate.</param>
        /// <param name="context">A context that provides information to evaluate which variant will be assigned to the user.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A variant assigned to the user based on the feature's configured allocation.</returns>
        public async ValueTask<Variant> GetVariantAsync(string feature, ITargetingContext context, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return (await EvaluateFeature(feature, context, useContext: true, cancellationToken).ConfigureAwait(false)).Variant;
        }

        private async ValueTask<EvaluationEvent> EvaluateFeature<TContext>(string feature, TContext context, bool useContext, CancellationToken cancellationToken)
        {
            var evaluationEvent = new EvaluationEvent
            {
                FeatureDefinition = await GetFeatureDefinition(feature).ConfigureAwait(false)
            };

            if (evaluationEvent.FeatureDefinition != null)
            {
                bool telemetryEnabled = evaluationEvent.FeatureDefinition.Telemetry?.Enabled ?? false;

                //
                // Only start an activity if telemetry is enabled for the feature
                using Activity activity = telemetryEnabled
                    ? ActivitySource.StartActivity("FeatureEvaluation")
                    : null;

                //
                // Determine IsEnabled
                evaluationEvent.Enabled = await IsEnabledAsync(evaluationEvent.FeatureDefinition, context, useContext, cancellationToken).ConfigureAwait(false);

                //
                // Determine Variant
                if (evaluationEvent.FeatureDefinition.Variants != null &&
                    evaluationEvent.FeatureDefinition.Variants.Any())
                {
                    VariantDefinition variantDefinition = null;

                    if (!useContext)
                    {
                        evaluationEvent.TargetingContext = await ResolveTargetingContextAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else if (context is ITargetingContext targetingInfo)
                    {
                        evaluationEvent.TargetingContext = new TargetingContext
                        {
                            UserId = targetingInfo.UserId,
                            Groups = targetingInfo.Groups
                        };
                    }

                    if (evaluationEvent.FeatureDefinition.Allocation == null)
                    {
                        evaluationEvent.VariantAssignmentReason = evaluationEvent.Enabled
                            ? VariantAssignmentReason.DefaultWhenEnabled
                            : VariantAssignmentReason.DefaultWhenDisabled;
                    }
                    else if (!evaluationEvent.Enabled)
                    {
                        if (evaluationEvent.FeatureDefinition.Allocation.DefaultWhenDisabled != null)
                        {
                            variantDefinition = evaluationEvent.FeatureDefinition
                                .Variants
                                .FirstOrDefault(variant =>
                                    variant.Name == evaluationEvent.FeatureDefinition.Allocation.DefaultWhenDisabled);
                        }

                        evaluationEvent.VariantAssignmentReason = VariantAssignmentReason.DefaultWhenDisabled;
                    }
                    else
                    {
                        if (evaluationEvent.TargetingContext == null)
                        {
                            if (useContext)
                            {
                                Logger?.LogWarning("The context of type {contextType} does not implement {targetingContextInterface} for variant assignment.", context.GetType().Name, nameof(ITargetingContext));
                            }
                            else if (TargetingContextAccessor == null)
                            {
                                Logger?.LogWarning("No instance of {targetingContextAccessorClass} could be found for variant assignment.", nameof(ITargetingContextAccessor));
                            }
                            else
                            {
                                Logger?.LogWarning("No instance of {targetingContextClass} could be found using {targetingContextAccessorClass} for variant assignment.", nameof(TargetingContext), nameof(ITargetingContextAccessor));
                            }
                        }

                        if (evaluationEvent.TargetingContext != null && evaluationEvent.FeatureDefinition.Allocation != null)
                        {
                            variantDefinition = await AssignVariantAsync(evaluationEvent, evaluationEvent.TargetingContext, cancellationToken).ConfigureAwait(false);
                        }

                        if (evaluationEvent.VariantAssignmentReason == VariantAssignmentReason.None)
                        {
                            if (evaluationEvent.FeatureDefinition.Allocation.DefaultWhenEnabled != null)
                            {
                                variantDefinition = evaluationEvent.FeatureDefinition
                                    .Variants
                                    .FirstOrDefault(variant =>
                                        variant.Name == evaluationEvent.FeatureDefinition.Allocation.DefaultWhenEnabled);
                            }

                            evaluationEvent.VariantAssignmentReason = VariantAssignmentReason.DefaultWhenEnabled;
                        }
                    }

                    evaluationEvent.Variant = variantDefinition != null ? GetVariantFromVariantDefinition(variantDefinition) : null;

                    //
                    // Override IsEnabled if variant has an override
                    if (variantDefinition != null && evaluationEvent.FeatureDefinition.Status != FeatureStatus.Disabled)
                    {
                        if (variantDefinition.StatusOverride == StatusOverride.Enabled)
                        {
                            evaluationEvent.Enabled = true;
                        }
                        else if (variantDefinition.StatusOverride == StatusOverride.Disabled)
                        {
                            evaluationEvent.Enabled = false;
                        }
                    }
                }

                if (_sessionManagers != null)
                {
                    foreach (ISessionManager sessionManager in _sessionManagers)
                    {
                        await sessionManager.SetAsync(evaluationEvent.FeatureDefinition.Name, evaluationEvent.Enabled).ConfigureAwait(false);
                    }
                }

                // Only add an activity event if telemetry is enabled for the feature and the activity is valid
                if (telemetryEnabled &&
                    Activity.Current != null &&
                    Activity.Current.IsAllDataRequested)
                {
                    AddEvaluationActivityEvent(evaluationEvent);
                }
            }

            return evaluationEvent;
        }

        private void AddEvaluationActivityEvent(EvaluationEvent evaluationEvent)
        {
            Debug.Assert(evaluationEvent != null);
            Debug.Assert(evaluationEvent.FeatureDefinition != null);

            // FeatureEvaluation event schema: https://github.com/microsoft/FeatureManagement/blob/main/Schema/FeatureEvaluationEvent/FeatureEvaluationEvent.v1.0.0.schema.json
            var tags = new ActivityTagsCollection()
            {
                { "FeatureName", evaluationEvent.FeatureDefinition.Name },
                { "Enabled", evaluationEvent.Enabled },
                { "VariantAssignmentReason", evaluationEvent.VariantAssignmentReason },
                { "Version", ActivitySource.Version }
            };

            if (!string.IsNullOrEmpty(evaluationEvent.TargetingContext?.UserId))
            {
                tags["TargetingId"] = evaluationEvent.TargetingContext.UserId;
            }

            if (!string.IsNullOrEmpty(evaluationEvent.Variant?.Name))
            {
                tags["Variant"] = evaluationEvent.Variant.Name;
            }

            if (evaluationEvent.FeatureDefinition.Telemetry.Metadata != null)
            {
                foreach (KeyValuePair<string, string> kvp in evaluationEvent.FeatureDefinition.Telemetry.Metadata)
                {
                    if (tags.ContainsKey(kvp.Key))
                    {
                        Logger?.LogWarning("{key} from telemetry metadata will be ignored, as it would override an existing key.", kvp.Key);

                        continue;
                    }

                    tags[kvp.Key] = kvp.Value;
                }
            }

            var activityEvent = new ActivityEvent("FeatureFlag", DateTimeOffset.UtcNow, tags);

            Activity.Current.AddEvent(activityEvent);
        }

        private async ValueTask<bool> IsEnabledAsync<TContext>(FeatureDefinition featureDefinition, TContext appContext, bool useAppContext, CancellationToken cancellationToken)
        {
            Debug.Assert(featureDefinition != null);

            if (_sessionManagers != null)
            {
                foreach (ISessionManager sessionManager in _sessionManagers)
                {
                    bool? readSessionResult = await sessionManager.GetAsync(featureDefinition.Name).ConfigureAwait(false);

                    if (readSessionResult.HasValue)
                    {
                        return readSessionResult.Value;
                    }
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
                    if (string.Equals(featureFilterConfiguration.Name, AlwaysOnFilterName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(featureFilterConfiguration.Name, OnFilterName, StringComparison.OrdinalIgnoreCase))
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
                        filter = GetFeatureFilterMetadata(featureFilterConfiguration.Name, appContext.GetType()) ??
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

                        if (!_options.IgnoreMissingFeatureFilters)
                        {
                            throw new FeatureManagementException(FeatureManagementError.MissingFeatureFilter, string.Format(FeatureFilterNotFoundError, featureFilterConfiguration.Name, featureDefinition.Name));
                        }

                        Logger?.LogWarning(FeatureFilterNotFoundError, featureFilterConfiguration.Name, featureDefinition.Name);

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
                        ContextualFeatureFilterEvaluator contextualFilter = GetContextualFeatureFilter(featureFilterConfiguration.Name, appContext.GetType());

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

        private async ValueTask<FeatureDefinition> GetFeatureDefinition(string feature)
        {
            FeatureDefinition featureDefinition = await _featureDefinitionProvider
                .GetFeatureDefinitionAsync(feature)
                .ConfigureAwait(false);

            if (featureDefinition == null)
            {
                if (!_options.IgnoreMissingFeatures)
                {
                    throw new FeatureManagementException(FeatureManagementError.MissingFeature, string.Format(FeatureDefinitionNotFoundError, feature));
                }

                if (Logger?.IsEnabled(LogLevel.Debug) == true)
                {
                    Logger.LogDebug(FeatureDefinitionNotFoundError, feature);
                }
            }

            return featureDefinition;
        }

        private async ValueTask<TargetingContext> ResolveTargetingContextAsync(CancellationToken cancellationToken)
        {
            if (TargetingContextAccessor == null)
            {
                return null;
            }

            //
            // Acquire targeting context via accessor
            TargetingContext context = await TargetingContextAccessor.GetContextAsync().ConfigureAwait(false);

            return context;
        }

        private ValueTask<VariantDefinition> AssignVariantAsync(EvaluationEvent evaluationEvent, TargetingContext targetingContext, CancellationToken cancellationToken)
        {
            Debug.Assert(evaluationEvent != null);

            Debug.Assert(targetingContext != null);

            Debug.Assert(evaluationEvent.FeatureDefinition.Allocation != null);

            VariantDefinition variant = null;

            if (evaluationEvent.FeatureDefinition.Allocation.User != null)
            {
                foreach (UserAllocation user in evaluationEvent.FeatureDefinition.Allocation.User)
                {
                    if (TargetingEvaluator.IsTargeted(targetingContext.UserId, user.Users, _assignerOptions.IgnoreCase))
                    {
                        if (string.IsNullOrEmpty(user.Variant))
                        {
                            Logger?.LogWarning("Missing variant name for user allocation in feature {featureName}", evaluationEvent.FeatureDefinition.Name);

                            return new ValueTask<VariantDefinition>((VariantDefinition)null);
                        }

                        Debug.Assert(evaluationEvent.FeatureDefinition.Variants != null);

                        evaluationEvent.VariantAssignmentReason = VariantAssignmentReason.User;

                        return new ValueTask<VariantDefinition>(
                            evaluationEvent.FeatureDefinition
                                .Variants
                                .FirstOrDefault(variant =>
                                    variant.Name == user.Variant));
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
                            Logger?.LogWarning("Missing variant name for group allocation in feature {featureName}", evaluationEvent.FeatureDefinition.Name);

                            return new ValueTask<VariantDefinition>((VariantDefinition)null);
                        }

                        Debug.Assert(evaluationEvent.FeatureDefinition.Variants != null);

                        evaluationEvent.VariantAssignmentReason = VariantAssignmentReason.Group;

                        return new ValueTask<VariantDefinition>(
                            evaluationEvent.FeatureDefinition
                                .Variants
                                .FirstOrDefault(variant =>
                                    variant.Name == group.Variant));
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
                            Logger?.LogWarning("Missing variant name for percentile allocation in feature {featureName}", evaluationEvent.FeatureDefinition.Name);

                            return new ValueTask<VariantDefinition>((VariantDefinition)null);
                        }

                        Debug.Assert(evaluationEvent.FeatureDefinition.Variants != null);

                        evaluationEvent.VariantAssignmentReason = VariantAssignmentReason.Percentile;

                        return new ValueTask<VariantDefinition>(
                            evaluationEvent.FeatureDefinition
                                .Variants
                                .FirstOrDefault(variant =>
                                    variant.Name == percentile.Variant));
                    }
                }
            }

            return new ValueTask<VariantDefinition>(variant);
        }

        private void BindSettings(IFeatureFilterMetadata filter, FeatureFilterEvaluationContext context, int filterIndex)
        {
            if (!(filter is IFilterParametersBinder binder))
            {
                return;
            }

            if (!(_featureDefinitionProvider is IFeatureDefinitionProviderCacheable) || Cache == null)
            {
                context.Settings = binder.BindParameters(context.Parameters);

                return;
            }

            object settings;

            string cacheKey = $"Microsoft.FeatureManagement{Environment.NewLine}{context.FeatureName}{Environment.NewLine}{filterIndex}";

            //
            // Check if settings already bound from configuration or the parameters have changed
            if (!Cache.TryGetValue(cacheKey, out ConfigurationCacheItem cacheItem) ||
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
                (_) =>
                {

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
            string name = ((FilterAliasAttribute)Attribute.GetCustomAttribute(filterType, typeof(FilterAliasAttribute)))?.Alias;

            if (name == null)
            {
                name = filterType.Name.EndsWith(FilterSuffix, StringComparison.OrdinalIgnoreCase) ? filterType.Name.Substring(0, filterType.Name.Length - FilterSuffix.Length) : filterType.Name;
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
                (_) =>
                {

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

        private Variant GetVariantFromVariantDefinition(VariantDefinition variantDefinition)
        {
            IConfigurationSection variantConfiguration = null;

            if (variantDefinition.ConfigurationValue.Exists())
            {
                variantConfiguration = variantDefinition.ConfigurationValue;
            }

            return new Variant()
            {
                Name = variantDefinition.Name,
                Configuration = variantConfiguration
            };
        }
    }
}
