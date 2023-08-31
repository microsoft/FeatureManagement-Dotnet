// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using System;

namespace Microsoft.FeatureManagement.FeatureFilters.Crontab
{
    /// <summary>
    /// The Crontab expression with Minute, Hour, Day of month, Month, Day of week fields.
    /// </summary>
    public class CrontabExpression
    {
        static private readonly int _numberOfFields = 5;

        private readonly CrontabField _minuteField = new CrontabField(CrontabFieldKind.Minute);
        private readonly CrontabField _hourField = new CrontabField(CrontabFieldKind.Hour);
        private readonly CrontabField _dayOfMonthField = new CrontabField(CrontabFieldKind.DayOfMonth);
        private readonly CrontabField _monthField = new CrontabField(CrontabFieldKind.Month);
        private readonly CrontabField _dayOfWeekField = new CrontabField(CrontabFieldKind.DayOfWeek);

        /// <summary>
        /// Checks whether the given expression can be parsed by the Crontab expression.
        /// </summary>
        /// <param name="expression">The expression to parse.</param>
        /// <param name="message">The error message.</param>
        /// <returns>True if the expression is valid and parsed successfully, otherwise false.</returns>
        public bool Parse(string expression, out string message)
        {
            if (expression == null)
            {
                message = $"Expression is null. Five fields in the sequence of Minute, Hour, Day of month, Month, and Day of week are required.";
                return false;
            }

            message = "";
            char[] whitespaceDelimiters = {' ', '\n', '\t'};
            string[] fields = expression.Split(whitespaceDelimiters, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length != _numberOfFields)
            {
                message = $"Expression: {expression} is invalid. Five fields in the sequence of Minute, Hour, Day of month, Month, and Day of week are required.";
                return false;
            }

            CrontabField[] fieldObjs = new CrontabField[]
            {
                _minuteField,
                _hourField,
                _dayOfMonthField,
                _monthField,
                _dayOfWeekField
            };

            for (int i = 0; i < fieldObjs.Length; i++)
            {
                if (fieldObjs[i].Parse(fields[i], out message) == false)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks whether the Crontab expression is satisfied by the given timestamp.
        /// </summary>
        /// <param name="time">The timestamp to check.</param>
        /// <returns>True if the Crontab expression is satisfied by the give timestamp, otherwise false.</returns>
        public bool IsSatisfiedBy(DateTimeOffset time)
        {
            bool isMatched;
            isMatched = _dayOfWeekField.Match((int)time.DayOfWeek)
                      & _monthField.Match((int)time.Month)
                      & _dayOfMonthField.Match((int)time.Day)
                      & _hourField.Match((int)time.Hour)
                      & _minuteField.Match((int)time.Minute);

            return isMatched;
        }
    }
}
