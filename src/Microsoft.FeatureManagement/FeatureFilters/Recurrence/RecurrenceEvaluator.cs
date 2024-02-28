// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    static class RecurrenceEvaluator
    {
        //
        // Error Message
        const string ValueOutOfRange = "The value is out of the accepted range.";
        const string UnrecognizableValue = "The value is unrecognizable.";
        const string RequiredParameter = "Value cannot be null or empty.";
        const string StartNotMatched = "Start date is not a valid first occurrence.";
        const string TimeWindowDurationOutOfRange = "Time window duration cannot be longer than how frequently it occurs";

        const int DaysPerWeek = 7;

        /// <summary>
        /// Checks if a provided timestamp is within any recurring time window specified by the Recurrence section in the time window filter settings.
        /// If the time window filter has an invalid recurrence setting, an exception will be thrown.
        /// <param name="time">A timestamp.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <returns>True if the timestamp is within any recurring time window, false otherwise.</returns>
        /// </summary>
        public static bool MatchRecurrence(DateTimeOffset time, TimeWindowFilterSettings settings)
        {
            if (time < settings.Start.Value)
            {
                return false;
            }

            if (TryFindPreviousOccurrence(time, settings, out DateTimeOffset previousOccurrence, out int _))
            {
                return time <= previousOccurrence + (settings.End.Value - settings.Start.Value);
            }

            return false;
        }

        /// <summary>
        /// Try to find the closest previous recurrence occurrence (if any) before the provided timestamp and the next occurrence behind it.
        /// <param name="time">A timestamp.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="prevOccurrence">The closest previous occurrence. If there is no previous occurrence, it will be set to <see cref="DateTimeOffset.MinValue"/>.</param>
        /// <param name="nextOccurrence">The next occurrence.</param>
        /// <returns>True if the closest previous occurrence is within the recurrence range or the time is before the first occurrence, false otherwise.</returns>
        /// </summary>
        public static bool TryFindPrevAndNextOccurrences(DateTimeOffset time, TimeWindowFilterSettings settings, out DateTimeOffset prevOccurrence, out DateTimeOffset nextOccurrence)
        {
            prevOccurrence = DateTimeOffset.MinValue;

            nextOccurrence = DateTimeOffset.MaxValue;

            if (time < settings.Start.Value)
            {
                //
                // The time is before the first occurrence.
                nextOccurrence = settings.Start.Value;

                return true;
            }

            if (TryFindPreviousOccurrence(time, settings, out prevOccurrence, out int numberOfOccurrences))
            {
                RecurrencePattern pattern = settings.Recurrence.Pattern;

                switch (pattern.Type)
                {
                    case RecurrencePatternType.Daily:
                        nextOccurrence = prevOccurrence.AddDays(pattern.Interval);

                        break;

                    case RecurrencePatternType.Weekly:
                        nextOccurrence = GetWeeklyNextOccurrence(prevOccurrence, settings);

                        break;

                    default:
                        return false;
                }

                RecurrenceRange range = settings.Recurrence.Range;

                if (range.Type == RecurrenceRangeType.EndDate)
                {
                    if (nextOccurrence > range.EndDate)
                    {
                        nextOccurrence = DateTimeOffset.MaxValue;
                    }
                }

                if (range.Type == RecurrenceRangeType.Numbered)
                {
                    if (numberOfOccurrences >= range.NumberOfOccurrences)
                    {
                        nextOccurrence = DateTimeOffset.MaxValue;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Perform validation of time window settings.
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="paramName">The name of the invalid setting, if any.</param>
        /// <param name="reason">The reason that the setting is invalid.</param>
        /// <returns>True if the provided settings are valid. False if the provided settings are invalid.</returns>
        /// </summary>
        public static bool TryValidateSettings(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.Recurrence != null)
            {
                return TryValidateRecurrenceRequiredParameter(settings, out paramName, out reason) &&
                    TryValidateRecurrencePattern(settings, out paramName, out reason) &&
                    TryValidateRecurrenceRange(settings, out paramName, out reason);
            }

            paramName = null;

            reason = null;

            return true;
        }

        private static bool TryValidateRecurrenceRequiredParameter(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            Debug.Assert(settings != null);
            Debug.Assert(settings.Recurrence != null);

            if (settings.Start == null)
            {
                paramName = nameof(settings.Start);

                reason = RequiredParameter;

                return false;
            }

            if (settings.End == null)
            {
                paramName = nameof(settings.End);

                reason = RequiredParameter;

                return false;
            }

            Recurrence recurrence = settings.Recurrence;

            if (recurrence.Pattern == null)
            {
                paramName = $"{nameof(settings.Recurrence)}.{nameof(recurrence.Pattern)}";

                reason = RequiredParameter;

                return false;
            }

            if (recurrence.Range == null)
            {
                paramName = $"{nameof(settings.Recurrence)}.{nameof(recurrence.Range)}";

                reason = RequiredParameter;

                return false;
            }

            if (settings.End.Value <= settings.Start.Value)
            {
                paramName = nameof(settings.End);

                reason = ValueOutOfRange;

                return false;
            }

            paramName = null;

            reason = null;

            return true;
        }

        private static bool TryValidateRecurrencePattern(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            Debug.Assert(settings != null);
            Debug.Assert(settings.Start != null);
            Debug.Assert(settings.End != null);
            Debug.Assert(settings.Recurrence != null);
            Debug.Assert(settings.Recurrence.Pattern != null);

            if (!TryValidateInterval(settings, out paramName, out reason))
            {
                return false;
            }

            switch (settings.Recurrence.Pattern.Type)
            {
                case RecurrencePatternType.Daily:
                    return TryValidateDailyRecurrencePattern(settings, out paramName, out reason);

                case RecurrencePatternType.Weekly:
                    return TryValidateWeeklyRecurrencePattern(settings, out paramName, out reason);

                default:
                    paramName = $"{nameof(settings.Recurrence)}.{nameof(settings.Recurrence.Pattern)}.{nameof(settings.Recurrence.Pattern.Type)}";

                    reason = UnrecognizableValue;

                    return false;
            }
        }

        private static bool TryValidateDailyRecurrencePattern(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            Debug.Assert(settings.Recurrence.Pattern.Interval >  0);

            //
            // No required parameter for "Daily" pattern
            // "Start" is always a valid first occurrence for "Daily" pattern

            TimeSpan intervalDuration = TimeSpan.FromDays(settings.Recurrence.Pattern.Interval);

            TimeSpan timeWindowDuration = settings.End.Value - settings.Start.Value;

            //
            // Time window duration must be shorter than how frequently it occurs
            if (timeWindowDuration > intervalDuration)
            {
                paramName = $"{nameof(settings.End)}";

                reason = TimeWindowDurationOutOfRange;

                return false;
            }

            paramName = null;

            reason = null;

            return true;
        }

        private static bool TryValidateWeeklyRecurrencePattern(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            RecurrencePattern pattern = settings.Recurrence.Pattern;

            Debug.Assert(pattern.Interval > 0);

            //
            // Required parameters
            if (!TryValidateDaysOfWeek(settings, out paramName, out reason))
            {
                return false;
            }

            TimeSpan intervalDuration = TimeSpan.FromDays(pattern.Interval * DaysPerWeek);

            TimeSpan timeWindowDuration = settings.End.Value - settings.Start.Value;

            //
            // Time window duration must be shorter than how frequently it occurs
            if (timeWindowDuration > intervalDuration || 
                !IsDurationCompliantWithDaysOfWeek(timeWindowDuration, pattern.Interval, pattern.DaysOfWeek, pattern.FirstDayOfWeek))
            {
                paramName = $"{nameof(settings.End)}";

                reason = TimeWindowDurationOutOfRange;

                return false;
            }

            //
            // Check whether "Start" is a valid first occurrence
            DateTimeOffset start = settings.Start.Value;

            if (!pattern.DaysOfWeek.Any(day =>
                day == start.DayOfWeek))
            {
                paramName = nameof(settings.Start);

                reason = StartNotMatched;

                return false;
            }

            return true;
        }

        private static bool TryValidateRecurrenceRange(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            Debug.Assert(settings != null);
            Debug.Assert(settings.Start != null);
            Debug.Assert(settings.Recurrence != null);
            Debug.Assert(settings.Recurrence.Range != null);

            switch(settings.Recurrence.Range.Type)
            {
                case RecurrenceRangeType.NoEnd:
                    paramName = null;

                    reason = null;

                    return true;

                case RecurrenceRangeType.EndDate:
                    return TryValidateEndDate(settings, out paramName, out reason);

                case RecurrenceRangeType.Numbered:
                    return TryValidateNumberOfOccurrences(settings, out paramName, out reason);

                default:
                    paramName = $"{nameof(settings.Recurrence)}.{nameof(settings.Recurrence.Range)}.{nameof(settings.Recurrence.Range.Type)}";

                    reason = UnrecognizableValue;

                    return false;
            }
        }

        private static bool TryValidateInterval(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            paramName = $"{nameof(settings.Recurrence)}.{nameof(settings.Recurrence.Pattern)}.{nameof(settings.Recurrence.Pattern.Interval)}";

            if (settings.Recurrence.Pattern.Interval <= 0)
            {
                reason = ValueOutOfRange;

                return false;
            }

            reason = null;

            return true;
        }

        private static bool TryValidateDaysOfWeek(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            paramName = $"{nameof(settings.Recurrence)}.{nameof(settings.Recurrence.Pattern)}.{nameof(settings.Recurrence.Pattern.DaysOfWeek)}";

            if (settings.Recurrence.Pattern.DaysOfWeek == null || !settings.Recurrence.Pattern.DaysOfWeek.Any())
            {
                reason = RequiredParameter;

                return false;
            }

            reason = null;

            return true;
        }

        private static bool TryValidateEndDate(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            paramName = $"{nameof(settings.Recurrence)}.{nameof(settings.Recurrence.Range)}.{nameof(settings.Recurrence.Range.EndDate)}";

            if (settings.Start == null)
            {
                paramName = nameof(settings.Start);

                reason = RequiredParameter;

                return false;
            }

            DateTimeOffset start = settings.Start.Value;

            DateTimeOffset endDate = settings.Recurrence.Range.EndDate;

            if (endDate < start)
            {
                reason = ValueOutOfRange;

                return false;
            }

            reason = null;

            return true;
        }

        private static bool TryValidateNumberOfOccurrences(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            paramName = $"{nameof(settings.Recurrence)}.{nameof(settings.Recurrence.Range)}.{nameof(settings.Recurrence.Range.NumberOfOccurrences)}";

            if (settings.Recurrence.Range.NumberOfOccurrences < 1)
            {
                reason = ValueOutOfRange;

                return false;
            }

            reason = null;

            return true;
        }

        /// <summary>
        /// Try to find the closest previous recurrence occurrence before the provided timestamp according to the recurrence pattern.
        /// <param name="time">A timestamp.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="previousOccurrence">The closest previous occurrence.</param>
        /// <param name="numberOfOccurrences">The number of occurrences between the time and the recurrence start.</param>
        /// <returns>True if the closest previous occurrence is within the recurrence range, false otherwise.</returns>
        /// </summary>
        private static bool TryFindPreviousOccurrence(DateTimeOffset time, TimeWindowFilterSettings settings, out DateTimeOffset previousOccurrence, out int numberOfOccurrences)
        {
            Debug.Assert(settings.Start != null);
            Debug.Assert(settings.Recurrence != null);
            Debug.Assert(settings.Recurrence.Pattern != null);
            Debug.Assert(settings.Recurrence.Range != null);
            Debug.Assert(settings.Start.Value <= time);

            previousOccurrence = DateTimeOffset.MinValue;

            numberOfOccurrences = 0;

            switch (settings.Recurrence.Pattern.Type)
            {
                case RecurrencePatternType.Daily:
                    FindDailyPreviousOccurrence(time, settings, out previousOccurrence, out numberOfOccurrences);

                    break;

                case RecurrencePatternType.Weekly:
                    FindWeeklyPreviousOccurrence(time, settings, out previousOccurrence, out numberOfOccurrences);

                    break;

                default:
                    return false;
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
        /// Find the closest previous recurrence occurrence before the provided timestamp according to the "Daily" recurrence pattern.
        /// <param name="time">A timestamp.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="previousOccurrence">The closest previous occurrence.</param>
        /// <param name="numberOfOccurrences">The number of occurrences between the time and the recurrence start.</param>
        /// </summary>
        private static void FindDailyPreviousOccurrence(DateTimeOffset time, TimeWindowFilterSettings settings, out DateTimeOffset previousOccurrence, out int numberOfOccurrences)
        {
            RecurrencePattern pattern = settings.Recurrence.Pattern;

            DateTimeOffset start = settings.Start.Value;

            Debug.Assert(time >= start);

            int interval = pattern.Interval;

            TimeSpan timeGap = time - start;

            //
            // netstandard2.0 does not support '/' operator for TimeSpan. After we stop supporting netstandard2.0, we can remove .TotalSeconds.
            int numberOfInterval = (int)Math.Floor(timeGap.TotalSeconds / TimeSpan.FromDays(interval).TotalSeconds);

            previousOccurrence = start.AddDays(numberOfInterval * interval);

            numberOfOccurrences = numberOfInterval + 1;
        }

        /// <summary>
        /// Find the closest previous recurrence occurrence before the provided timestamp according to the "Weekly" recurrence pattern.
        /// <param name="time">A timestamp.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="previousOccurrence">The closest previous occurrence.</param>
        /// <param name="numberOfOccurrences">The number of occurrences between the time and the recurrence start.</param>
        /// </summary>
        private static void FindWeeklyPreviousOccurrence(DateTimeOffset time, TimeWindowFilterSettings settings, out DateTimeOffset previousOccurrence, out int numberOfOccurrences)
        {
            RecurrencePattern pattern = settings.Recurrence.Pattern;

            DateTimeOffset start = settings.Start.Value;

            Debug.Assert(time >= start);

            int interval = pattern.Interval;

            DateTimeOffset firstDayOfStartWeek = start.AddDays(
                -CalculateWeeklyDayOffset(start.DayOfWeek, pattern.FirstDayOfWeek));

            //
            // netstandard2.0 does not support '/' operator for TimeSpan. After we stop supporting netstandard2.0, we can remove .TotalSeconds.
            int numberOfInterval = (int)Math.Floor((time - firstDayOfStartWeek).TotalSeconds / TimeSpan.FromDays(interval * DaysPerWeek).TotalSeconds);

            DateTimeOffset firstDayOfMostRecentOccurringWeek = firstDayOfStartWeek.AddDays(numberOfInterval * (interval * DaysPerWeek));

            List<DayOfWeek> sortedDaysOfWeek = SortDaysOfWeek(pattern.DaysOfWeek, pattern.FirstDayOfWeek);

            //
            // substract the day before the start in the first week
            numberOfOccurrences = numberOfInterval * sortedDaysOfWeek.Count - sortedDaysOfWeek.IndexOf(start.DayOfWeek);

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
        /// Find the next recurrence occurrence after the provided previous occurrence according to the "Weekly" recurrence pattern.
        /// <param name="previousOccurrence">The previous occurrence.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// </summary>
        private static DateTimeOffset GetWeeklyNextOccurrence(DateTimeOffset previousOccurrence, TimeWindowFilterSettings settings)
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
                pattern.Interval * DaysPerWeek - CalculateWeeklyDayOffset(previousOccurrence.DayOfWeek, sortedDaysOfWeek.First()));
        }

        /// <summary>
        /// Check whether the duration is shorter than the minimum gap between recurrence of days of week.
        /// </summary>
        /// <param name="duration">The time span of the duration.</param>
        /// <param name="interval">The recurrence interval.</param>
        /// <param name="daysOfWeek">The days of the week when the recurrence will occur.</param>
        /// <param name="firstDayOfWeek">The first day of the week.</param>
        /// <returns>True if the duration is compliant with days of week, false otherwise.</returns>
        private static bool IsDurationCompliantWithDaysOfWeek(TimeSpan duration, int interval, IEnumerable<DayOfWeek> daysOfWeek, DayOfWeek firstDayOfWeek)
        {
            Debug.Assert(interval > 0);

            if (daysOfWeek.Count() == 1)
            {
                return true;
            }

            DateTime firstDayOfThisWeek = DateTime.Today.AddDays(
                DaysPerWeek - CalculateWeeklyDayOffset(DateTime.Today.DayOfWeek, firstDayOfWeek));

            List<DayOfWeek> sortedDaysOfWeek = SortDaysOfWeek(daysOfWeek, firstDayOfWeek);

            DateTime prev = DateTime.MinValue;

            TimeSpan minGap = TimeSpan.FromDays(DaysPerWeek);

            foreach(DayOfWeek dayOfWeek in sortedDaysOfWeek)
            {
                if (prev == DateTime.MinValue)
                {
                    prev = firstDayOfThisWeek.AddDays(
                        CalculateWeeklyDayOffset(dayOfWeek, firstDayOfWeek));
                }
                else
                {
                    DateTime date = firstDayOfThisWeek.AddDays(
                        CalculateWeeklyDayOffset(dayOfWeek, firstDayOfWeek));

                    TimeSpan gap = date - prev;

                    if (gap < minGap)
                    {
                        minGap = gap;
                    }

                    prev = date;
                }
            }

            //
            // It may across weeks. Check the next week if the interval is one week.
            if (interval == 1)
            {
                DateTime firstDayOfNextWeek = firstDayOfThisWeek.AddDays(DaysPerWeek);

                DateTime firstOccurrenceInNextWeek = firstDayOfNextWeek.AddDays(
                    CalculateWeeklyDayOffset(sortedDaysOfWeek.First(), firstDayOfWeek));

                TimeSpan gap = firstOccurrenceInNextWeek - prev;

                if (gap < minGap)
                {
                    minGap = gap;
                }
            }

            return minGap >= duration;
        }

        private static int CalculateWeeklyDayOffset(DayOfWeek day1, DayOfWeek day2)
        {
            return ((int)day1 - (int)day2 + DaysPerWeek) % DaysPerWeek;
        }

        private static List<DayOfWeek> SortDaysOfWeek(IEnumerable<DayOfWeek> daysOfWeek, DayOfWeek firstDayOfWeek)
        {
            List<DayOfWeek> result = daysOfWeek.ToList();

            result.Sort((x, y) =>
                CalculateWeeklyDayOffset(x, firstDayOfWeek)
                    .CompareTo(
                        CalculateWeeklyDayOffset(y, firstDayOfWeek)));

            return result;
        }
    }
}
