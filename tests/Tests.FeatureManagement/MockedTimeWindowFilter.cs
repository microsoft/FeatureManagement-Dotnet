using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Tests.FeatureManagement
{
    public class MockedTimeWindowFilter
    {
        private readonly ConcurrentDictionary<TimeWindowFilterSettings, DateTimeOffset?> _recurrenceCache;

        public MockedTimeWindowFilter()
        {
            _recurrenceCache = new ConcurrentDictionary<TimeWindowFilterSettings, DateTimeOffset?>();
        }

        public bool Evaluate(DateTimeOffset now, FeatureFilterEvaluationContext context)
        {
            Debug.Assert(context.Settings != null);

            TimeWindowFilterSettings settings = (TimeWindowFilterSettings)context.Settings;

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
                DateTimeOffset? closestStart = _recurrenceCache.GetOrAdd(settings, RecurrenceEvaluator.CalculateClosestStart(now, settings));

                if (closestStart == null || now < closestStart.Value)
                {
                    return false;
                }

                if (now < closestStart.Value + (settings.End.Value - settings.Start.Value))
                {
                    return true;
                }

                closestStart = RecurrenceEvaluator.CalculateClosestStart(now, settings);

                _recurrenceCache.AddOrUpdate(
                    settings,
                    (_) => throw new KeyNotFoundException(),
                    (_, _) => closestStart);

                if (closestStart == null || now < closestStart.Value)
                {
                    return false;
                }

                return now < closestStart.Value + (settings.End.Value - settings.Start.Value);
            }

            return false;
        }
    }
}
