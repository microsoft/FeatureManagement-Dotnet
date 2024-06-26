// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    internal static class RecurrenceEvaluator
    {
        private const int DaysPerWeek = 7;

        /// <summary>
        /// Checks if a provided timestamp is within any recurring time window specified by the Recurrence section in the time window filter settings.
        /// If the time window filter has an invalid recurrence setting, an exception will be thrown.
        /// <param name="time">A datetime.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <returns>True if the timestamp is within any recurring time window, false otherwise.</returns>
        /// </summary>
        public static bool IsMatch(DateTimeOffset time, TimeWindowFilterSettings settings)
        {
            Debug.Assert(settings != null);
            Debug.Assert(settings.Start != null);
            Debug.Assert(settings.Recurrence != null);
            Debug.Assert(settings.Recurrence.Pattern != null);
            Debug.Assert(settings.Recurrence.Range != null);

            if (time < settings.Start.Value)
            {
                return false;
            }

            if (TryFindPreviousOccurrence(time, settings, out DateTimeOffset previousOccurrence, out int _))
            {
                return time < previousOccurrence + (settings.End.Value - settings.Start.Value);
            }

            return false;
        }

        /// <summary>
        /// Calculates the start time of the closest active time window.
        /// <param name="time">A datetime.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <returns>The start time of the closest active time window or null if the recurrence range surpasses its end.</returns>
        /// </summary>
        public static DateTimeOffset? CalculateClosestStart(DateTimeOffset time, TimeWindowFilterSettings settings)
        {
            CalculateSurroundingOccurrences(time, settings, out DateTimeOffset? prevOccurrence, out DateTimeOffset? nextOccurrence);

            if (time < settings.Start.Value)
            {
                return nextOccurrence.Value;
            }

            if (prevOccurrence != null)
            {
                bool isWithinPreviousTimeWindow =
                        time < prevOccurrence.Value + (settings.End.Value - settings.Start.Value);

                if (isWithinPreviousTimeWindow)
                {
                    return prevOccurrence.Value;
                }

                if (nextOccurrence != null)
                {
                    return nextOccurrence.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Calculates the closest previous recurrence occurrence (if any) before the given time and the next occurrence (if any) after it.
        /// <param name="time">A datetime.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="prevOccurrence">The closest previous occurrence. Note that prev occurrence can be null even if the time is past the start date, because the recurrence range may have surpassed its end.</param>
        /// <param name="nextOccurrence">The next occurrence. Note that next occurrence can be null even if the prev occurrence is not null, because the recurrence range may have reached its end.</param>
        /// </summary>
        private static void CalculateSurroundingOccurrences(DateTimeOffset time, TimeWindowFilterSettings settings, out DateTimeOffset? prevOccurrence, out DateTimeOffset? nextOccurrence)
        {
            Debug.Assert(settings != null);
            Debug.Assert(settings.Start != null);
            Debug.Assert(settings.Recurrence != null);
            Debug.Assert(settings.Recurrence.Pattern != null);
            Debug.Assert(settings.Recurrence.Range != null);

            prevOccurrence = null;

            nextOccurrence = null;

            if (time < settings.Start.Value)
            {
                nextOccurrence = settings.Start.Value;

                return;
            }

            if (TryFindPreviousOccurrence(time, settings, out DateTimeOffset prev, out int numberOfOccurrences))
            {
                prevOccurrence = prev;

                RecurrencePattern pattern = settings.Recurrence.Pattern;

                if (pattern.Type == RecurrencePatternType.Daily)
                {
                    nextOccurrence = prev.AddDays(pattern.Interval);
                }

                if (pattern.Type == RecurrencePatternType.Weekly)
                {
                    nextOccurrence = CalculateWeeklyNextOccurrence(prev, settings);
                }

                RecurrenceRange range = settings.Recurrence.Range;

                if (range.Type == RecurrenceRangeType.EndDate)
                {
                    if (nextOccurrence > range.EndDate)
                    {
                        nextOccurrence = null;
                    }
                }

                if (range.Type == RecurrenceRangeType.Numbered)
                {
                    if (numberOfOccurrences >= range.NumberOfOccurrences)
                    {
                        nextOccurrence = null;
                    }
                }
            }
        }

        /// <summary>
        /// Finds the closest previous recurrence occurrence before the given time according to the recurrence pattern. 
        /// The given time should be later than the recurrence start. 
        /// A return value indicates whether any previous occurrence can be found.
        /// <param name="time">A datetime.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="previousOccurrence">The closest previous occurrence.</param>
        /// <param name="numberOfOccurrences">The number of occurrences between the time and the recurrence start.</param>
        /// <returns>True if the closest previous occurrence is within the recurrence range, false otherwise.</returns>
        /// </summary>
        private static bool TryFindPreviousOccurrence(DateTimeOffset time, TimeWindowFilterSettings settings, out DateTimeOffset previousOccurrence, out int numberOfOccurrences)
        {
            Debug.Assert(settings.Start.Value <= time);

            previousOccurrence = DateTimeOffset.MinValue;

            numberOfOccurrences = 0;

            RecurrencePattern pattern = settings.Recurrence.Pattern;

            if (pattern.Type == RecurrencePatternType.Daily)
            {
                FindDailyPreviousOccurrence(time, settings, out previousOccurrence, out numberOfOccurrences);
            }

            if (pattern.Type == RecurrencePatternType.Weekly)
            {
                FindWeeklyPreviousOccurrence(time, settings, out previousOccurrence, out numberOfOccurrences);
            }

            RecurrenceRange range = settings.Recurrence.Range;

            if (range.Type == RecurrenceRangeType.EndDate)
            {
                return previousOccurrence <= range.EndDate;
            }

            if (range.Type == RecurrenceRangeType.Numbered)
            {
                return numberOfOccurrences <= range.NumberOfOccurrences;
            }

            return true;
        }

        /// <summary>
        /// Finds the closest previous recurrence occurrence before the given time according to the "Daily" recurrence pattern. 
        /// The given time should be later than the recurrence start.
        /// <param name="time">A datetime.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="previousOccurrence">The closest previous occurrence.</param>
        /// <param name="numberOfOccurrences">The number of occurrences between the time and the recurrence start.</param>
        /// </summary>
        private static void FindDailyPreviousOccurrence(DateTimeOffset time, TimeWindowFilterSettings settings, out DateTimeOffset previousOccurrence, out int numberOfOccurrences)
        {
            Debug.Assert(settings.Start.Value <= time);

            RecurrencePattern pattern = settings.Recurrence.Pattern;

            DateTimeOffset start = settings.Start.Value;

            int interval = pattern.Interval;

            TimeSpan timeGap = time - start;

            //
            // netstandard2.0 does not support '/' operator for TimeSpan. After we stop supporting netstandard2.0, we can remove .TotalSeconds.
            int numberOfInterval = (int)Math.Floor(timeGap.TotalSeconds / TimeSpan.FromDays(interval).TotalSeconds);

            previousOccurrence = start.AddDays(numberOfInterval * interval);

            numberOfOccurrences = numberOfInterval + 1;
        }

        /// <summary>
        /// Finds the closest previous recurrence occurrence before the given time according to the "Weekly" recurrence pattern. 
        /// The given time should be later than the recurrence start.
        /// <param name="time">A datetime.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="previousOccurrence">The closest previous occurrence.</param>
        /// <param name="numberOfOccurrences">The number of occurrences between the time and the recurrence start.</param>
        /// </summary>
        private static void FindWeeklyPreviousOccurrence(DateTimeOffset time, TimeWindowFilterSettings settings, out DateTimeOffset previousOccurrence, out int numberOfOccurrences)
        {
            Debug.Assert(settings.Start.Value <= time);

            RecurrencePattern pattern = settings.Recurrence.Pattern;

            DateTimeOffset start = settings.Start.Value;

            int interval = pattern.Interval;

            DateTimeOffset firstDayOfStartWeek = start.AddDays(
                -CalculateWeeklyDayOffset(start.DayOfWeek, pattern.FirstDayOfWeek));

            //
            // netstandard2.0 does not support '/' operator for TimeSpan. After we stop supporting netstandard2.0, we can remove .TotalSeconds.
            int numberOfInterval = (int)Math.Floor((time - firstDayOfStartWeek).TotalSeconds / TimeSpan.FromDays(interval * DaysPerWeek).TotalSeconds);

            DateTimeOffset firstDayOfMostRecentOccurringWeek = firstDayOfStartWeek.AddDays(numberOfInterval * (interval * DaysPerWeek));

            List<DayOfWeek> sortedDaysOfWeek = SortDaysOfWeek(pattern.DaysOfWeek, pattern.FirstDayOfWeek);

            //
            // Subtract the days before the start in the first week.
            numberOfOccurrences = (numberOfInterval * sortedDaysOfWeek.Count) - sortedDaysOfWeek.IndexOf(start.DayOfWeek);

            //
            // The current time is not within the most recent occurring week.
            if (time - firstDayOfMostRecentOccurringWeek > TimeSpan.FromDays(DaysPerWeek))
            {
                numberOfOccurrences += sortedDaysOfWeek.Count;

                //
                // day with max offset in the most recent occurring week
                previousOccurrence = firstDayOfMostRecentOccurringWeek.AddDays(
                    CalculateWeeklyDayOffset(sortedDaysOfWeek.Last(), pattern.FirstDayOfWeek));

                return;
            }

            //
            // day with the min offset in the most recent occurring week
            DateTimeOffset dayWithMinOffset = firstDayOfMostRecentOccurringWeek.AddDays(
                CalculateWeeklyDayOffset(sortedDaysOfWeek.First(), pattern.FirstDayOfWeek));

            if (dayWithMinOffset < start)
            {
                numberOfOccurrences = 0;

                dayWithMinOffset = start;
            }

            if (time >= dayWithMinOffset)
            {
                previousOccurrence = dayWithMinOffset;

                numberOfOccurrences++;

                //
                // Find the day with the max offset that is less than the current time.
                for (int i = sortedDaysOfWeek.IndexOf(dayWithMinOffset.DayOfWeek) + 1; i < sortedDaysOfWeek.Count; i++)
                {
                    DateTimeOffset dayOfWeek = firstDayOfMostRecentOccurringWeek.AddDays(
                        CalculateWeeklyDayOffset(sortedDaysOfWeek[i], pattern.FirstDayOfWeek));

                    if (time < dayOfWeek)
                    {
                        break;
                    }

                    previousOccurrence = dayOfWeek;

                    numberOfOccurrences++;
                }
            }
            else
            {
                //
                // the previous occurring week
                DateTimeOffset firstDayOfPreviousOccurringWeek = firstDayOfMostRecentOccurringWeek.AddDays(-interval * DaysPerWeek);

                //
                // day with max offset in the last occurring week
                previousOccurrence = firstDayOfPreviousOccurringWeek.AddDays(
                    CalculateWeeklyDayOffset(sortedDaysOfWeek.Last(), pattern.FirstDayOfWeek));
            }
        }

        /// <summary>
        /// Finds the next recurrence occurrence after the provided previous occurrence according to the "Weekly" recurrence pattern.
        /// <param name="previousOccurrence">The previous occurrence.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// </summary>
        private static DateTimeOffset CalculateWeeklyNextOccurrence(DateTimeOffset previousOccurrence, TimeWindowFilterSettings settings)
        {
            RecurrencePattern pattern = settings.Recurrence.Pattern;

            List<DayOfWeek> sortedDaysOfWeek = SortDaysOfWeek(pattern.DaysOfWeek, pattern.FirstDayOfWeek);

            int i = sortedDaysOfWeek.IndexOf(previousOccurrence.DayOfWeek) + 1;

            if (i < sortedDaysOfWeek.Count())
            {
                return previousOccurrence.AddDays(
                    CalculateWeeklyDayOffset(sortedDaysOfWeek[i], previousOccurrence.DayOfWeek));
            }

            return previousOccurrence.AddDays(
                (pattern.Interval * DaysPerWeek) - CalculateWeeklyDayOffset(previousOccurrence.DayOfWeek, sortedDaysOfWeek.First()));
        }

        /// <summary>
        /// Calculates the offset in days between two given days of the week.
        /// <param name="day1">A day of week.</param>
        /// <param name="day2">A day of week.</param>
        /// <returns>The number of days to be added to day2 to reach day1</returns>
        /// </summary>
        private static int CalculateWeeklyDayOffset(DayOfWeek day1, DayOfWeek day2)
        {
            return ((int)day1 - (int)day2 + DaysPerWeek) % DaysPerWeek;
        }

        /// <summary>
        /// Sorts a collection of days of week based on their offsets from a specified first day of week.
        /// <param name="daysOfWeek">A collection of days of week.</param>
        /// <param name="firstDayOfWeek">The first day of week.</param>
        /// <returns>The sorted days of week.</returns>
        /// </summary>
        private static List<DayOfWeek> SortDaysOfWeek(IEnumerable<DayOfWeek> daysOfWeek, DayOfWeek firstDayOfWeek)
        {
            var result = daysOfWeek.ToList();

            result.Sort((x, y) =>
                CalculateWeeklyDayOffset(x, firstDayOfWeek)
                    .CompareTo(
                        CalculateWeeklyDayOffset(y, firstDayOfWeek)));

            return result;
        }
    }
}
