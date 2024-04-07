// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const string Alias = "Microsoft.TimeWindow";
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<TimeWindowFilterSettings, DateTimeOffset> _recurrenceCache;

        /// <summary>
        /// Creates a time window based feature filter.
        /// </summary>
        /// <param name="loggerFactory">A logger factory for creating loggers.</param>
        public TimeWindowFilter(ILoggerFactory loggerFactory = null)
        {
            _logger = loggerFactory?.CreateLogger<TimeWindowFilter>();
            _recurrenceCache = new ConcurrentDictionary<TimeWindowFilterSettings, DateTimeOffset>();
        }

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
                if (context.Settings == null)
                {
                    return Task.FromResult(RecurrenceEvaluator.MatchRecurrence(now, settings));
                }

                DateTimeOffset cachedTime = _recurrenceCache.GetOrAdd(
                    settings,
                    (_) =>
                    {
                        RecurrenceEvaluator.CalculateSurroundingOccurrences(now, settings, out DateTimeOffset? prevOccurrence, out DateTimeOffset? nextOccurrence);

                        if (now < settings.Start.Value)
                        {
                            return nextOccurrence.Value;
                        }

                        if (prevOccurrence != null)
                        {
                            return prevOccurrence.Value;
                        }

                        //
                        // There is no previous occurrence within the reccurrence range.
                        return DateTimeOffset.MaxValue;
                    });

                if (now < cachedTime)
                {
                    return Task.FromResult(false);
                }

                if (now < cachedTime + (settings.End.Value - settings.Start.Value))
                {
                    return Task.FromResult(true);
                }

                RecurrenceEvaluator.CalculateSurroundingOccurrences(now, settings, out DateTimeOffset? prevOccurrence, out DateTimeOffset? nextOccurrrence);

                if (prevOccurrence != null)
                {
                    Debug.Assert(now > settings.Start.Value);

                    bool isWithinPreviousTimeWindow =
                        now <= prevOccurrence.Value + (settings.End.Value - settings.Start.Value);

                    _recurrenceCache.AddOrUpdate(
                        settings,
                        (_) => throw new KeyNotFoundException(),
                        (_, _) => isWithinPreviousTimeWindow ?
                            prevOccurrence.Value :
                            nextOccurrrence ?? DateTimeOffset.MaxValue);

                    return Task.FromResult(isWithinPreviousTimeWindow);
                }

                //
                // There is no previous occurrence within the recurrence range.
                _recurrenceCache.AddOrUpdate(
                    settings,
                    (_) => throw new KeyNotFoundException(),
                    (_, _) => DateTimeOffset.MaxValue);
            }

            return Task.FromResult(false);
        }
    }
}
