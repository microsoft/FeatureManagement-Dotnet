// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    static class RecurrenceValidator
    {
        const int DaysPerWeek = 7;

        //
        // Error Message
        const string ValueOutOfRange = "The value is out of the accepted range.";
        const string UnrecognizableValue = "The value is unrecognizable.";
        const string RequiredParameter = "Value cannot be null or empty.";
        const string StartNotMatched = "Start date is not a valid first occurrence.";
        const string TimeWindowDurationOutOfRange = "Time window duration cannot be longer than how frequently it occurs";

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
            Debug.Assert(settings.Recurrence.Pattern.Interval > 0);

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

            switch (settings.Recurrence.Range.Type)
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

            foreach (DayOfWeek dayOfWeek in sortedDaysOfWeek)
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

        /// <summary>
        /// Calculate the offset in days between two given days of the week.
        /// <param name="day1">A day of week.</param>
        /// <param name="day2">A day of week.</param>
        /// <returns>The number of days to be added to day2 to reach day1</returns>
        /// </summary>
        private static int CalculateWeeklyDayOffset(DayOfWeek day1, DayOfWeek day2)
        {
            return ((int)day1 - (int)day2 + DaysPerWeek) % DaysPerWeek;
        }


        /// <summary>
        /// Sort a collection of days of week based on their offsets from a specified first day of week.
        /// <param name="daysOfWeek">A collection of days of week.</param>
        /// <param name="firstDayOfWeek">The first day of week.</param>
        /// <returns>The sorted days of week.</returns>
        /// </summary>
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
