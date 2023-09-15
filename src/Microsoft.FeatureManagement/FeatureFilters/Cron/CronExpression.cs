// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;

namespace Microsoft.FeatureManagement.FeatureFilters.Cron
{
    /// <summary>
    /// The Crontab expression with Minute, Hour, Day of month, Month, Day of week fields.
    /// </summary>
    internal class CronExpression
    {
        private static readonly int NumberOfFields = 5;
        private static readonly char[] WhitespaceDelimiters = {' ', '\n', '\t'};

        private readonly CronField _minute;
        private readonly CronField _hour;
        private readonly CronField _dayOfMonth;
        private readonly CronField _month;
        private readonly CronField _dayOfWeek;

        public CronExpression(CronField minute, CronField hour, CronField dayOfMonth, CronField month, CronField dayOfWeek)
        {
            _minute = minute;
            _hour = hour;
            _dayOfMonth = dayOfMonth;
            _month = month;
            _dayOfWeek = dayOfWeek;
        }

        /// <summary>
        /// Check whether the given expression can be parsed by a CronExpression.
        /// If the expression is invalid, an ArgumentException will be thrown.
        /// </summary>
        /// <param name="expression">The expression to parse.</param>
        /// <returns>A parsed CronExpression.</returns>
        public static CronExpression Parse(string expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            string InvalidCronExpressionErrorMessage = $"The provided Cron expression: '{expression}' is invalid.";

            string[] fields = expression.Split(WhitespaceDelimiters, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length != NumberOfFields)
            {
                throw new ArgumentException(InvalidCronExpressionErrorMessage, nameof(expression));
            }

            if (CronField.TryParse(CronFieldKind.Minute, fields[0], out CronField minute)
                && CronField.TryParse(CronFieldKind.Hour, fields[1], out CronField hour)
                && CronField.TryParse(CronFieldKind.DayOfMonth, fields[2], out CronField dayOfMonth)
                && CronField.TryParse(CronFieldKind.Month, fields[3], out CronField month)
                && CronField.TryParse(CronFieldKind.DayOfWeek, fields[4], out CronField dayOfWeek))
            {
                return new CronExpression(minute, hour, dayOfMonth, month, dayOfWeek);
            }
            else
            {
                throw new ArgumentException(InvalidCronExpressionErrorMessage, nameof(expression));
            }
        }

        /// <summary>
        /// Checks whether the Crontab expression is satisfied by the given timestamp.
        /// </summary>
        /// <param name="time">The timestamp to check.</param>
        /// <returns>True if the Crontab expression is satisfied by the give timestamp, otherwise false.</returns>
        public bool IsSatisfiedBy(DateTimeOffset time)
        {
            /*
            The curent time is said to be satisfied by the Crontab when the 'minute', 'hour', and 'month of the year' fields match the current time, 
            and at least one of the two 'day' fields ('day of month', or 'day of week') match the current time.
            If both 'day' fields are restricted (i.e., do not contain the "*" character), the current time will be considered as satisfied when it match either 'day' field and other fields.
            If exactly one of 'day' fields are restricted, the current time will be considered as satisfied when it match both 'day' fields and other fields.
             */
            bool isDayMatched;
            if (!_dayOfMonth.MatchAll && !_dayOfWeek.MatchAll)
            {
                isDayMatched = (_dayOfMonth.Match((int)time.Day) || _dayOfWeek.Match((int)time.DayOfWeek))
                            && _month.Match((int)time.Month);
            }
            else
            {
                isDayMatched = _dayOfMonth.Match((int)time.Day)
                            && _dayOfWeek.Match((int)time.DayOfWeek)
                            && _month.Match((int)time.Month);
            }

            bool isTimeSpanMatched = _hour.Match((int)time.Hour) && _minute.Match((int)time.Minute);

            bool isMatched = isDayMatched && isTimeSpanMatched;

            return isMatched;
        }
    }
}
