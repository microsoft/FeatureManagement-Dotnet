// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// A feature filter that can be used to activate a feature based on a time window.
    /// The time window can be configured to recur periodically.
    /// </summary>
    [FilterAlias(Alias)]
    public class TimeWindowFilter : IFeatureFilter, IFilterParametersBinder
    {
        private readonly TimeSpan ParametersCacheSlidingExpiration = TimeSpan.FromMinutes(5);
        private readonly TimeSpan ParametersCacheAbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

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
        public IMemoryCache Cache { get; init; }

        /// <summary>
        /// This property allows the time window filter in our test suite to use simulated current time.
        /// </summary>
        internal ITimeProvider TimeProvider { get; init; }

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
            //
            // Check if prebound settings available, otherwise bind from parameters.
            TimeWindowFilterSettings settings = (TimeWindowFilterSettings)context.Settings ?? (TimeWindowFilterSettings)BindParameters(context.Parameters);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (TimeProvider != null)
            {
                now = TimeProvider.GetTime();
            }

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
                // The reference of the object will be used for hash key.
                // If there is no pre-bounded settings attached to the context, there will be no cached filter settings and each call will have a unique settings object.
                // In this case, the cache for recurrence settings won't work.
                if (context.Settings == null || Cache == null)
                {
                    return Task.FromResult(RecurrenceEvaluator.IsMatch(now, settings));
                }

                //
                // The start time of the closest active time window. It could be null if the recurrence range surpasses its end.
                DateTimeOffset? closestStart;

                if (!Cache.TryGetValue(settings, out closestStart))
                {
                    closestStart = RecurrenceEvaluator.CalculateClosestStart(now, settings);

                    Cache.Set(
                        settings, 
                        closestStart, 
                        new MemoryCacheEntryOptions
                        {
                            SlidingExpiration = ParametersCacheSlidingExpiration,
                            AbsoluteExpirationRelativeToNow = ParametersCacheAbsoluteExpirationRelativeToNow,
                            Size = 1
                        });
                }

                if (closestStart == null || now < closestStart.Value)
                {
                    return Task.FromResult(false);
                }

                if (now < closestStart.Value + (settings.End.Value - settings.Start.Value))
                {
                    return Task.FromResult(true);
                }

                closestStart = RecurrenceEvaluator.CalculateClosestStart(now, settings);

                Cache.Set(
                    settings,
                    closestStart,
                    new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = ParametersCacheSlidingExpiration,
                        AbsoluteExpirationRelativeToNow = ParametersCacheAbsoluteExpirationRelativeToNow,
                        Size = 1
                    });

                if (closestStart == null || now < closestStart.Value)
                {
                    return Task.FromResult(false);
                }

                return Task.FromResult(now < closestStart.Value + (settings.End.Value - settings.Start.Value));
            }

            return Task.FromResult(false);
        }
    }
}
