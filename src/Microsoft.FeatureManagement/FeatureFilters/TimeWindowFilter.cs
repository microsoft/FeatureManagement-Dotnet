// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// A feature filter that can be used to activate a feature based on a time window.
    /// The time window can be configured to recur periodically.
    /// </summary>
    [FilterAlias(Alias)]
    public class TimeWindowFilter : IFeatureFilter, IFilterParametersBinder
    {
        private readonly TimeSpan CacheSlidingExpiration = TimeSpan.FromMinutes(5);
        private readonly TimeSpan CacheAbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

        private const string Alias = "Microsoft.TimeWindow";
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a time window based feature filter.
        /// </summary>
        /// <param name="loggerFactory">A logger factory for creating loggers.</param>
        public TimeWindowFilter(ILoggerFactory loggerFactory = null)
        {
            _logger = loggerFactory?.CreateLogger<TimeWindowFilter>();
        }

        /// <summary>
        /// The application memory cache to store the start time of the closest active time window. By caching this time, the time window can minimize redundant computations when evaluating recurrence.
        /// </summary>
        public IMemoryCache Cache { get; set; }

        /// <summary>
        /// This property allows the time window filter in our test suite to use simulated time.
        /// </summary>
        internal ISystemClock SystemClock { get; set; }

        /// <summary>
        /// Binds configuration representing filter parameters to <see cref="TimeWindowFilterSettings"/>.
        /// </summary>
        /// <param name="filterParameters">The configuration representing filter parameters that should be bound to <see cref="TimeWindowFilterSettings"/>.</param>
        /// <returns><see cref="TimeWindowFilterSettings"/> that can later be used in feature evaluation.</returns>
        public object BindParameters(IConfiguration filterParameters)
        {
            var settings = filterParameters.Get<TimeWindowFilterSettings>() ?? new TimeWindowFilterSettings();

            if (!RecurrenceValidator.TryValidateSettings(settings, out string paramName, out string reason))
            {
                throw new ArgumentException(reason, paramName);
            }

            return settings;
        }

        /// <summary>
        /// Evaluates whether a feature is enabled based on the <see cref="TimeWindowFilterSettings"/> specified in the configuration.
        /// </summary>
        /// <param name="context">The feature evaluation context.</param>
        /// <returns>True if the feature is enabled, false otherwise.</returns>
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            //
            // Check if prebound settings available, otherwise bind from parameters.
            TimeWindowFilterSettings settings = (TimeWindowFilterSettings)context.Settings ?? (TimeWindowFilterSettings)BindParameters(context.Parameters);

            DateTimeOffset now = SystemClock?.UtcNow ?? DateTimeOffset.UtcNow;

            if (!settings.Start.HasValue && !settings.End.HasValue)
            {
                _logger?.LogWarning($"The '{Alias}' feature filter is not valid for feature '{context.FeatureName}'. It must specify either '{nameof(settings.Start)}', '{nameof(settings.End)}', or both.");

                return Task.FromResult(false);
            }

            //
            // Hit the first occurrence of the time window
            if ((!settings.Start.HasValue || now >= settings.Start.Value) && (!settings.End.HasValue || now < settings.End.Value))
            {
                return Task.FromResult(true);
            }

            if (settings.Recurrence != null)
            {
                //
                // The reference of the object will be used for cache key.
                // If there is no pre-bounded settings attached to the context, there will be no cached filter settings and each call will have a unique settings object.
                // In this case, the cache for recurrence settings won't work.
                if (Cache == null || context.Settings == null)
                {
                    return Task.FromResult(RecurrenceEvaluator.IsMatch(now, settings));
                }

                //
                // The start time of the closest active time window. It could be null if the recurrence range surpasses its end.
                DateTimeOffset? closestStart;

                TimeSpan activeDuration = settings.End.Value - settings.Start.Value;

                //
                // Recalculate the closest start if not yet calculated,
                // Or if we have passed the cached time window.
                if (!Cache.TryGetValue(settings, out closestStart) ||
                    (closestStart.HasValue && now >= closestStart.Value + activeDuration))
                {
                    closestStart = ReloadClosestStart(settings);
                }

                if (!closestStart.HasValue || now < closestStart.Value)
                {
                    return Task.FromResult(false);
                }

                return Task.FromResult(now < closestStart.Value + activeDuration);
            }

            return Task.FromResult(false);
        }

        private DateTimeOffset? ReloadClosestStart(TimeWindowFilterSettings settings)
        {
            DateTimeOffset now = SystemClock?.UtcNow ?? DateTimeOffset.UtcNow;

            DateTimeOffset? closestStart = RecurrenceEvaluator.CalculateClosestStart(now, settings);

            Cache.Set(
                settings,
                closestStart,
                new MemoryCacheEntryOptions
                {
                    SlidingExpiration = CacheSlidingExpiration,
                    AbsoluteExpirationRelativeToNow = CacheAbsoluteExpirationRelativeToNow,
                    Size = 1
                });

            return closestStart;
        }
    }
}
