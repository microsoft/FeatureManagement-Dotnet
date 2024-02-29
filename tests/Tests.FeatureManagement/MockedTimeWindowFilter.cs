using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    public class MockedTimeWindowFilter
    {
        private readonly ConcurrentDictionary<TimeWindowFilterSettings, DateTimeOffset> _recurrenceCache;

        public MockedTimeWindowFilter()
        {
            _recurrenceCache = new ConcurrentDictionary<TimeWindowFilterSettings, DateTimeOffset>();
        }

        public object BindParameters(IConfiguration filterParameters)
        {
            var settings = filterParameters.Get<TimeWindowFilterSettings>() ?? new TimeWindowFilterSettings();

            if (!RecurrenceEvaluator.TryValidateSettings(settings, out string paramName, out string reason))
            {
                throw new ArgumentException(reason, paramName);
            }

            return settings;
        }

        public bool Evaluate(DateTimeOffset now, FeatureFilterEvaluationContext context)
        {
            //
            // Check if prebound settings available, otherwise bind from parameters.
            TimeWindowFilterSettings settings = (TimeWindowFilterSettings)context.Settings ?? (TimeWindowFilterSettings)BindParameters(context.Parameters);

            if (!settings.Start.HasValue && !settings.End.HasValue)
            {
                return false;
            }

            //
            // Hit the first occurrence of the time window
            if ((!settings.Start.HasValue || now >= settings.Start.Value) && (!settings.End.HasValue || now < settings.End.Value))
            {
                return true;
            }

            if (settings.Recurrence != null)
            {
                //
                // The reference of the object will be used for hash key.
                // If there is no pre-bounded settings attached to the context, there will be no cached filter settings and each call will have a unique settings object.
                // In this case, the cache for recurrence settings won't work.
                if (context.Settings != null)
                {
                    DateTimeOffset cachedTime = _recurrenceCache.GetOrAdd(
                        settings,
                        (_) =>
                        {
                            if (RecurrenceEvaluator.TryFindPrevAndNextOccurrences(now, settings, out DateTimeOffset prevOccurrence, out DateTimeOffset _))
                            {
                                return prevOccurrence;
                            }

                            //
                            // There is no previous occurrence within the reccurrence range.
                            return DateTimeOffset.MaxValue;
                        });

                    if (now < cachedTime)
                    {
                        return false;
                    }

                    if (now <= cachedTime + (settings.End.Value - settings.Start.Value))
                    {
                        return true;
                    }

                    if (RecurrenceEvaluator.TryFindPrevAndNextOccurrences(now, settings, out DateTimeOffset prevOccurrence, out DateTimeOffset nextOccurrrence))
                    {
                        bool isWithinPreviousTimeWindow =
                            now <= prevOccurrence + (settings.End.Value - settings.Start.Value);

                        _recurrenceCache.AddOrUpdate(
                            settings,
                            (_) => throw new KeyNotFoundException(),
                            (_, _) => isWithinPreviousTimeWindow ?
                                prevOccurrence :
                                nextOccurrrence);

                        return isWithinPreviousTimeWindow;
                    }

                    //
                    // There is no previous occurrence within the reccurrence range.
                    _recurrenceCache.AddOrUpdate(
                        settings,
                        (_) => throw new KeyNotFoundException(),
                        (_, _) => DateTimeOffset.MaxValue);

                    return false;
                }

                return RecurrenceEvaluator.MatchRecurrence(now, settings);
            }

            return false;
        }
    }
}
