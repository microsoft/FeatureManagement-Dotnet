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

        const int DayNumberOfWeek = 7;
        const int MinDayNumberOfMonth = 28;
        const int MinDayNumberOfYear = 365;

        /// <summary>
        /// Checks if a provided timestamp is within any recurring time window specified by the Recurrence section in the time window filter settings.
        /// If the time window filter has an invalid recurrence setting, an exception will be thrown.
        /// <param name="time">A time stamp.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <returns>True if the time stamp is within any recurring time window, false otherwise.</returns>
        /// </summary>
        public static bool MatchRecurrence(DateTimeOffset time, TimeWindowFilterSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (!TryValidateRecurrenceSettings(settings, out string paramName, out string reason))
            {
                throw new ArgumentException(reason, paramName);
            }

            if (time < settings.Start.Value)
            {
                return false;
            }

            // time is before the first occurrence or time is beyond the end of the recurrence range
            if (!TryGetPreviousOccurrence(time, settings, out DateTimeOffset previousOccurrence))
            {
                return false;
            }

            if (time <= previousOccurrence + (settings.End.Value - settings.Start.Value))
            {
                return true;
            }

            return false;
        }

        private static bool TryValidateRecurrenceSettings(TimeWindowFilterSettings settings, out string paramName, out string reason)
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

            if (settings.End.Value - settings.Start.Value <= TimeSpan.Zero)
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

                case RecurrencePatternType.AbsoluteMonthly:
                    return TryValidateAbsoluteMonthlyRecurrencePattern(settings, out paramName, out reason);

                case RecurrencePatternType.RelativeMonthly:
                    return TryValidateRelativeMonthlyRecurrencePattern(settings, out paramName, out reason);

                case RecurrencePatternType.AbsoluteYearly:
                    return TryValidateAbsoluteYearlyRecurrencePattern(settings, out paramName, out reason);

                case RecurrencePatternType.RelativeYearly:
                    return TryValidateRelativeYearlyRecurrencePattern(settings, out paramName, out reason);

                default:
                    paramName = $"{nameof(settings.Recurrence)}.{nameof(settings.Recurrence.Pattern)}.{nameof(settings.Recurrence.Pattern.Type)}";

                    reason = UnrecognizableValue;

                    return false;
            }
        }

        private static bool TryValidateDailyRecurrencePattern(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            Debug.Assert(settings.Recurrence.Pattern.Interval >  0);

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

            //
            // No required parameter for "Daily" pattern
            // "Start" is always a valid first occurrence for "Daily" pattern

            paramName = null;

            reason = null;

            return true;
        }

        private static bool TryValidateWeeklyRecurrencePattern(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            RecurrencePattern pattern = settings.Recurrence.Pattern;

            Debug.Assert(pattern.Interval > 0);

            TimeSpan intervalDuration = TimeSpan.FromDays(pattern.Interval * DayNumberOfWeek);

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
            // Required parameters
            if (!TryValidateDaysOfWeek(settings, out paramName, out reason))
            {
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

        private static bool TryValidateAbsoluteMonthlyRecurrencePattern(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            RecurrencePattern pattern = settings.Recurrence.Pattern;

            Debug.Assert(pattern.Interval > 0);

            TimeSpan intervalDuration = TimeSpan.FromDays(pattern.Interval * MinDayNumberOfMonth);

            TimeSpan timeWindowDuration = settings.End.Value - settings.Start.Value;

            //
            // Time window duration must be shorter than how frequently it occurs
            if (timeWindowDuration > intervalDuration)
            {
                paramName = $"{nameof(settings.End)}";

                reason = TimeWindowDurationOutOfRange;

                return false;
            }

            //
            // Required parameters
            if (!TryValidateDayOfMonth(settings, out paramName, out reason))
            {
                return false;
            }

            //
            // Check whether "Start" is a valid first occurrence
            DateTimeOffset start = settings.Start.Value;

            if (start.Day != pattern.DayOfMonth.Value)
            {
                paramName = nameof(settings.Start);

                reason = StartNotMatched;

                return false;
            }

            return true;
        }

        private static bool TryValidateRelativeMonthlyRecurrencePattern(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            RecurrencePattern pattern = settings.Recurrence.Pattern;

            Debug.Assert(pattern.Interval > 0);

            TimeSpan intervalDuration = TimeSpan.FromDays(pattern.Interval * MinDayNumberOfMonth);

            TimeSpan timeWindowDuration = settings.End.Value - settings.Start.Value;

            //
            // Time window duration must be shorter than how frequently it occurs
            if (timeWindowDuration > intervalDuration)
            {
                paramName = $"{nameof(settings.End)}";

                reason = TimeWindowDurationOutOfRange;

                return false;
            }

            //
            // Required parameters
            if (!TryValidateDaysOfWeek(settings, out paramName, out reason))
            {
                return false;
            }

            //
            // Check whether "Start" is a valid first occurrence
            DateTimeOffset start = settings.Start.Value;

            if (!pattern.DaysOfWeek.Any(day =>
                NthDayOfWeekInTheMonth(start.DateTime, pattern.Index, day) == start.Date))
            {
                paramName = nameof(settings.Start);

                reason = StartNotMatched;

                return false;
            }

            return true;
        }

        private static bool TryValidateAbsoluteYearlyRecurrencePattern(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            RecurrencePattern pattern = settings.Recurrence.Pattern;

            Debug.Assert(pattern.Interval > 0);

            TimeSpan intervalDuration = TimeSpan.FromDays(pattern.Interval * MinDayNumberOfYear);

            TimeSpan timeWindowDuration = settings.End.Value - settings.Start.Value;

            //
            // Time window duration must be shorter than how frequently it occurs
            if (timeWindowDuration > intervalDuration)
            {
                paramName = $"{nameof(settings.End)}";

                reason = TimeWindowDurationOutOfRange;

                return false;
            }

            //
            // Required parameters
            if (!TryValidateMonth(settings, out paramName, out reason))
            {
                return false;
            }

            if (!TryValidateDayOfMonth(settings, out paramName, out reason))
            {
                return false;
            }

            //
            // Check whether "Start" is a valid first occurrence
            DateTimeOffset start = settings.Start.Value;

            if (start.Day != pattern.DayOfMonth.Value || start.Month != pattern.Month.Value)
            {
                paramName = nameof(settings.Start);

                reason = StartNotMatched;

                return false;
            }

            return true;
        }

        private static bool TryValidateRelativeYearlyRecurrencePattern(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            RecurrencePattern pattern = settings.Recurrence.Pattern;

            Debug.Assert(pattern.Interval > 0);

            TimeSpan intervalDuration = TimeSpan.FromDays(pattern.Interval * MinDayNumberOfYear);

            TimeSpan timeWindowDuration = settings.End.Value - settings.Start.Value;

            //
            // Time window duration must be shorter than how frequently it occurs
            if (timeWindowDuration > intervalDuration)
            {
                paramName = $"{nameof(settings.End)}";

                reason = TimeWindowDurationOutOfRange;

                return false;
            }

            //
            // Required parameters
            if (!TryValidateDaysOfWeek(settings, out paramName, out reason))
            {
                return false;
            }

            if (!TryValidateMonth(settings, out paramName, out reason))
            {
                return false;
            }

            //
            // Check whether "Start" is a valid first occurrence
            DateTimeOffset start = settings.Start.Value;

            if (start.Month != pattern.Month.Value ||
                    !pattern.DaysOfWeek.Any(day =>
                        NthDayOfWeekInTheMonth(start.DateTime, pattern.Index, day) == start.Date))
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

        private static bool TryValidateDayOfMonth(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            paramName = $"{nameof(settings.Recurrence)}.{nameof(settings.Recurrence.Pattern)}.{nameof(settings.Recurrence.Pattern.DayOfMonth)}";

            if (settings.Recurrence.Pattern.DayOfMonth == null)
            {
                reason = RequiredParameter;

                return false;
            }

            if (settings.Recurrence.Pattern.DayOfMonth < 1 || settings.Recurrence.Pattern.DayOfMonth > 31)
            {
                reason = ValueOutOfRange;

                return false;
            }

            reason = null;

            return true;
        }

        private static bool TryValidateMonth(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            paramName = $"{nameof(settings.Recurrence)}.{nameof(settings.Recurrence.Pattern)}.{nameof(settings.Recurrence.Pattern.Month)}";

            if (settings.Recurrence.Pattern.Month == null)
            {
                reason = RequiredParameter;

                return false;
            }

            if (settings.Recurrence.Pattern.Month < 1 || settings.Recurrence.Pattern.Month > 12)
            {
                reason = ValueOutOfRange;

                return false;
            }

            reason = null;

            return true;
        }

        private static bool TryValidateEndDate(TimeWindowFilterSettings settings, out string paramName, out string reason)
        {
            paramName = $"{nameof(settings.Recurrence)}.{nameof(settings.Recurrence.Range)}.{nameof(settings.Recurrence.Range.EndDate)}";

            if (settings.Recurrence.Range.EndDate == null)
            {
                reason = RequiredParameter;

                return false;
            }

            if (settings.Start == null)
            {
                paramName = nameof(settings.Start);

                reason = RequiredParameter;

                return false;
            }

            DateTimeOffset start = settings.Start.Value;

            DateTimeOffset endDate = settings.Recurrence.Range.EndDate.Value;

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
        /// Try to find the closest previous recurrence occurrence before the provided time stamp according to the recurrence pattern.
        /// <param name="time">A time stamp.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="previousOccurrence">The closest previous occurrence.</param>
        /// <returns>True if the closest previous occurrence is within the recurrence range, false otherwise.</returns>
        /// </summary>
        private static bool TryGetPreviousOccurrence(DateTimeOffset time, TimeWindowFilterSettings settings, out DateTimeOffset previousOccurrence)
        {
            Debug.Assert(settings.Start != null);
            Debug.Assert(settings.Recurrence != null);
            Debug.Assert(settings.Recurrence.Pattern != null);
            Debug.Assert(settings.Recurrence.Range != null);

            previousOccurrence = DateTimeOffset.MaxValue;

            DateTimeOffset start = settings.Start.Value;

            if (time < start)
            {
                return false;
            }

            int numberOfOccurrences;

            switch (settings.Recurrence.Pattern.Type)
            {
                case RecurrencePatternType.Daily:
                    FindDailyPreviousOccurrence(time, settings, out previousOccurrence, out numberOfOccurrences);

                    break;

                case RecurrencePatternType.Weekly:
                    FindWeeklyPreviousOccurrence(time, settings, out previousOccurrence, out numberOfOccurrences);

                    break;

                case RecurrencePatternType.AbsoluteMonthly:
                    FindAbsoluteMonthlyPreviousOccurrence(time, settings, out previousOccurrence, out numberOfOccurrences);

                    break;

                case RecurrencePatternType.RelativeMonthly:
                    FindRelativeMonthlyPreviousOccurrence(time, settings, out previousOccurrence, out numberOfOccurrences);

                    break;

                case RecurrencePatternType.AbsoluteYearly:
                    FindAbsoluteYearlyPreviousOccurrence(time, settings, out previousOccurrence, out numberOfOccurrences);

                    break;

                case RecurrencePatternType.RelativeYearly:
                    FindRelativeYearlyPreviousOccurrence(time, settings, out previousOccurrence, out numberOfOccurrences);

                    break;

                default:
                    return false;
            }

            RecurrenceRange range = settings.Recurrence.Range;

            if (range.Type == RecurrenceRangeType.EndDate)
            {
                return previousOccurrence <= range.EndDate.Value;
            }

            if (range.Type == RecurrenceRangeType.Numbered)
            {
                return numberOfOccurrences <= range.NumberOfOccurrences;
            }

            return true;
        }

        /// <summary>
        /// Find the closest previous recurrence occurrence before the provided time stamp according to the "Daily" recurrence pattern.
        /// <param name="time">A time stamp.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="previousOccurrence">The closest previous occurrence.</param>
        /// <param name="numberOfOccurrences">The number of complete recurrence intervals which have occurred between the time and the recurrence start.</param>
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
            int numberOfInterval = (int) Math.Floor(timeGap.TotalSeconds / TimeSpan.FromDays(interval).TotalSeconds);

            previousOccurrence = start.AddDays(numberOfInterval * interval);

            numberOfOccurrences = numberOfInterval + 1;
        }

        /// <summary>
        /// Find the closest previous recurrence occurrence before the provided time stamp according to the "Weekly" recurrence pattern.
        /// <param name="time">A time stamp.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="previousOccurrence">The closest previous occurrence.</param>
        /// <param name="numberOfOccurrences">The number of recurring days of week which have occurred between the time and the recurrence start.</param>
        /// </summary>
        private static void FindWeeklyPreviousOccurrence(DateTimeOffset time, TimeWindowFilterSettings settings, out DateTimeOffset previousOccurrence, out int numberOfOccurrences)
        {
            RecurrencePattern pattern = settings.Recurrence.Pattern;

            DateTimeOffset start = settings.Start.Value;

            Debug.Assert(time >= start);

            int interval = pattern.Interval;

            TimeSpan timeGap = time - start;

            int remainingDaysOfFirstWeek = RemainingDaysOfTheWeek(start.DayOfWeek, pattern.FirstDayOfWeek);

            TimeSpan remainingTimeOfFirstWeek = TimeSpan.FromDays(remainingDaysOfFirstWeek) - start.TimeOfDay;

            TimeSpan remaingTimeOfFirstIntervalAfterFirstWeek = TimeSpan.FromDays((interval - 1) * DayNumberOfWeek);

            TimeSpan remainingTimeOfFirstInterval = remainingTimeOfFirstWeek + remaingTimeOfFirstIntervalAfterFirstWeek;

            DateTimeOffset tentativePreviousOccurrence = start;

            numberOfOccurrences = 0;

            //
            // Time is not within the first interval
            if (remainingTimeOfFirstInterval <= timeGap)
            {
                //
                // Add the occurrence in the first week and shift the tentative previous occurrence to the next week
                while (tentativePreviousOccurrence.DayOfWeek != pattern.FirstDayOfWeek || 
                    tentativePreviousOccurrence == start)
                {
                    if (pattern.DaysOfWeek.Any(day =>
                        day == tentativePreviousOccurrence.DayOfWeek))
                    {
                        numberOfOccurrences += 1;
                    }

                    tentativePreviousOccurrence += TimeSpan.FromDays(1);
                }

                //
                // Shift the tentative previous occurrence to the first day of the first week of the second interval
                tentativePreviousOccurrence += remaingTimeOfFirstIntervalAfterFirstWeek;

                //
                // The number of intervals between the first and the latest intervals (not inclusive)
                // netstandard2.0 does not support '/' operator for TimeSpan. After we stop supporting netstandard2.0, we can remove .TotalSeconds.
                int numberOfInterval = (int) Math.Floor((timeGap - remainingTimeOfFirstInterval).TotalSeconds / TimeSpan.FromDays(interval * DayNumberOfWeek).TotalSeconds);

                //
                // Shift the tentative previous occurrence to the first day of the first week of the latest interval
                tentativePreviousOccurrence += TimeSpan.FromDays(numberOfInterval * interval * DayNumberOfWeek);

                //
                // Add the occurrence in the intervals between the first and the latest intervals (not inclusive)
                numberOfOccurrences += numberOfInterval * pattern.DaysOfWeek.Count();
            }

            //
            // Tentative previous occurrence should either be the start or the first day of the first week of the latest interval.
            previousOccurrence = tentativePreviousOccurrence;

            //
            // Check the following days of the first week if time is still within the first interval
            // Otherwise, check the first week of the latest interval
            while (tentativePreviousOccurrence <= time)
            {
                if (pattern.DaysOfWeek.Any(day =>
                    day == tentativePreviousOccurrence.DayOfWeek))
                {
                    previousOccurrence = tentativePreviousOccurrence;

                    numberOfOccurrences += 1;
                }

                tentativePreviousOccurrence += TimeSpan.FromDays(1);

                if (tentativePreviousOccurrence.DayOfWeek == pattern.FirstDayOfWeek)
                {
                    //
                    // It comes to the next week, so break.
                    break;
                }
            }

            Console.WriteLine("YES");
        }

        /// <summary>
        /// Find the closest previous recurrence occurrence before the provided time stamp according to the "AbsoluteMonthly" recurrence pattern.
        /// <param name="time">A time stamp.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="previousOccurrence">The closest previous occurrence.</param>
        /// <param name="numberOfOccurrences">The number of complete recurrence intervals which have occurred between the time and the recurrence start.</param>
        /// </summary>
        private static void FindAbsoluteMonthlyPreviousOccurrence(DateTimeOffset time, TimeWindowFilterSettings settings, out DateTimeOffset previousOccurrence, out int numberOfOccurrences)
        {
            RecurrencePattern pattern = settings.Recurrence.Pattern;

            DateTimeOffset start = settings.Start.Value;

            Debug.Assert(time >= start);

            int interval = pattern.Interval;

            DateTime alignedTime = time.DateTime + start.Offset - time.Offset;

            int monthGap = (alignedTime.Year - start.Year) * 12 + alignedTime.Month - start.Month;

            if (alignedTime.TimeOfDay + TimeSpan.FromDays(alignedTime.Day) < start.TimeOfDay + TimeSpan.FromDays(start.Day))
            {
                //
                // E.g. start: 2023-9-1T12:00:00 and time: 2023-10-1T11:00:00
                // Not a complete month
                monthGap -= 1;
            }

            int numberOfInterval = monthGap / interval;

            previousOccurrence = start.AddMonths(numberOfInterval * interval);

            numberOfOccurrences = numberOfInterval + 1;
        }

        /// <summary>
        /// Find the closest previous recurrence occurrence before the provided time stamp according to the "RelativeMonthly" recurrence pattern.
        /// <param name="time">A time stamp.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="previousOccurrence">The closest previous occurrence.</param>
        /// <param name="numberOfOccurrences">The number of complete recurrence intervals which have occurred between the time and the recurrence start.</param>
        /// </summary>
        private static void FindRelativeMonthlyPreviousOccurrence(DateTimeOffset time, TimeWindowFilterSettings settings, out DateTimeOffset previousOccurrence, out int numberOfOccurrences)
        {
            RecurrencePattern pattern = settings.Recurrence.Pattern;

            DateTimeOffset start = settings.Start.Value;

            Debug.Assert(time >= start);

            int interval = pattern.Interval;

            DateTime alignedTime = time.DateTime + start.Offset - time.Offset;

            int monthGap = (alignedTime.Year - start.Year) * 12 + alignedTime.Month - start.Month;

            if (!pattern.DaysOfWeek.Any(day =>
                alignedTime >= NthDayOfWeekInTheMonth(alignedTime, pattern.Index, day) + start.TimeOfDay))
            {
                //
                // E.g. start is 2023.9.1 (the first Friday in 2023.9) and current time is 2023.10.2 (the first Friday in next month is 2023.10.6)
                // Not a complete month
                monthGap -= 1;
            }

            int numberOfInterval = monthGap / interval;

            DateTime previousOccurrenceMonth = start.AddMonths(numberOfInterval * interval).DateTime;

            //
            // Find the first occurence date matched the pattern
            // Only one day of week in the month will be matched
            previousOccurrence = DateTimeOffset.MaxValue;

            foreach (DayOfWeek day in pattern.DaysOfWeek)
            {
                DateTime occurrenceDate = NthDayOfWeekInTheMonth(previousOccurrenceMonth, pattern.Index, day);

                if (occurrenceDate + start.TimeOfDay < previousOccurrence)
                {
                    previousOccurrence = occurrenceDate + start.TimeOfDay;
                }
            }

            numberOfOccurrences = numberOfInterval + 1;
        }

        /// <summary>
        /// Find the closest previous recurrence occurrence before the provided time stamp according to the "AbsoluteYearly" recurrence pattern.
        /// <param name="time">A time stamp.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="previousOccurrence">The closest previous occurrence.</param>
        /// <param name="numberOfOccurrences">The number of complete recurrence intervals which have occurred between the time and the recurrence start.</param>
        /// </summary>
        private static void FindAbsoluteYearlyPreviousOccurrence(DateTimeOffset time, TimeWindowFilterSettings settings, out DateTimeOffset previousOccurrence, out int numberOfOccurrences)
        {
            RecurrencePattern pattern = settings.Recurrence.Pattern;

            DateTimeOffset start = settings.Start.Value;

            Debug.Assert(time >= start);

            int interval = pattern.Interval;

            DateTime alignedTime = time.DateTime + start.Offset - time.Offset;

            int yearGap = alignedTime.Year - start.Year;

            if (alignedTime.TimeOfDay + TimeSpan.FromDays(alignedTime.DayOfYear) < start.TimeOfDay + TimeSpan.FromDays(start.DayOfYear))
            {
                //
                // E.g. start: 2023-9-1T12:00:00 and time: 2024-9-1T11:00:00
                // Not a complete year
                yearGap -= 1;
            }

            int numberOfInterval = yearGap / interval;

            previousOccurrence = start.AddYears(numberOfInterval * interval);

            numberOfOccurrences = numberOfInterval + 1;
        }

        /// <summary>
        /// Find the closest previous recurrence occurrence before the provided time stamp according to the "RelativeYearly" recurrence pattern.
        /// <param name="time">A time stamp.</param>
        /// <param name="settings">The settings of time window filter.</param>
        /// <param name="previousOccurrence">The closest previous occurrence.</param>
        /// <param name="numberOfOccurrences">The number of complete recurrence intervals which have occurred between the time and the recurrence start.</param>
        /// </summary>
        private static void FindRelativeYearlyPreviousOccurrence(DateTimeOffset time, TimeWindowFilterSettings settings, out DateTimeOffset previousOccurrence, out int numberOfOccurrences)
        {
            RecurrencePattern pattern = settings.Recurrence.Pattern;

            DateTimeOffset start = settings.Start.Value;

            Debug.Assert(time >= start);

            int interval = pattern.Interval;

            DateTime alignedTime = time.DateTime + start.Offset - time.Offset;

            int yearGap = alignedTime.Year - start.Year;

            if (alignedTime.Month < start.Month)
            {
                //
                // E.g. start: 2023.9 and time: 2024.8
                // Not a complete year
                yearGap -= 1;
            }
            else if (alignedTime.Month == start.Month && 
                !pattern.DaysOfWeek.Any(day =>
                    alignedTime >= NthDayOfWeekInTheMonth(alignedTime, pattern.Index, day) + start.TimeOfDay))
            {
                //
                // E.g. start: 2023.9.1 (the first Friday in 2023.9) and time: 2024.9.2 (the first Friday in 2023.9 is 2024.9.6)
                // Not a complete year
                yearGap -= 1;
            }

            int numberOfInterval = yearGap / interval;

            DateTime previousOccurrenceMonth = start.AddYears(numberOfInterval * interval).DateTime;

            //
            // Find the first occurence date matched the pattern
            // Only one day of week in the month will be matched
            previousOccurrence = DateTime.MaxValue;

            foreach (DayOfWeek day in pattern.DaysOfWeek)
            {
                DateTime occurrenceDate = NthDayOfWeekInTheMonth(previousOccurrenceMonth, pattern.Index, day);

                if (occurrenceDate + start.TimeOfDay < previousOccurrence)
                {
                    previousOccurrence = occurrenceDate + start.TimeOfDay;
                }
            }

            numberOfOccurrences = numberOfInterval + 1;
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

            // Shift to the first day of the week
            DateTime date = DateTime.Today.AddDays(
                RemainingDaysOfTheWeek(DateTime.Today.DayOfWeek, firstDayOfWeek));

            DateTime prev = DateTime.MinValue;

            TimeSpan minGap = TimeSpan.FromDays(DayNumberOfWeek);

            for (int i = 0; i < DayNumberOfWeek; i++)
            {
                if (daysOfWeek.Any(day =>
                    day == date.DayOfWeek))
                {
                    if (prev == DateTime.MinValue)
                    {
                        //
                        // Find a occurrence for the first time
                        prev = date;
                    }
                    else
                    {
                        TimeSpan gap = date - prev;

                        if (gap < minGap)
                        {
                            minGap = gap;
                        }

                        prev = date;
                    }
                }

                date += TimeSpan.FromDays(1);
            }

            //
            // It may across weeks. Check the next week if the interval is one week.
            if (interval == 1)
            {
                for (int i = 0; i < DayNumberOfWeek; i++)
                {
                    if (daysOfWeek.Any(day =>
                        day == date.DayOfWeek))
                    {
                        TimeSpan gap = date - prev;

                        if (gap < minGap)
                        {
                            minGap = gap;
                        }

                        //
                        // Only check the first occurrence in the next week
                        break;
                    }

                    date += TimeSpan.FromDays(1);
                }
            }

            return minGap >= duration;
        }

        private static int RemainingDaysOfTheWeek(DayOfWeek dayOfWeek, DayOfWeek firstDayOfWeek)
        {
            int remainingDays = (int) dayOfWeek - (int) firstDayOfWeek;

            if (remainingDays < 0)
            {
                return -remainingDays;
            }
            else
            {
                return DayNumberOfWeek - remainingDays;
            }
        }

        /// <summary>
        /// Find the nth day of week in the month of the date time.
        /// </summary>
        /// <param name="dateTime">A date time.</param>
        /// <param name="index">The index of the day of week in the month.</param>
        /// <param name="dayOfWeek">The day of week.</param>
        /// <returns>The data time of the nth day of week in the month.</returns>
        private static DateTime NthDayOfWeekInTheMonth(DateTime dateTime, WeekIndex index, DayOfWeek dayOfWeek)
        {
            var date = new DateTime(dateTime.Year, dateTime.Month, 1);

            //
            // Find the first provided day of week in the month
            while (date.DayOfWeek != dayOfWeek)
            {
                date += TimeSpan.FromDays(1);
            }

            if (date.AddDays(DayNumberOfWeek * (int) index).Month == dateTime.Month)
            {
                date += TimeSpan.FromDays(DayNumberOfWeek * (int) index);
            }
            else // There is no the 5th day of week in the month
            {
                //
                // Add 3 weeks to reach the fourth day of week in the month
                date += TimeSpan.FromDays(DayNumberOfWeek * 3);
            }

            return date;
        }
    }
}
