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
        private static readonly int NumberOfFields = 5;
        private static readonly char[] WhitespaceDelimiters = {' ', '\n', '\t'};

        private readonly CrontabField[] CrontabFields = new CrontabField[]
        {
            new CrontabField(CrontabFieldKind.Minute),
            new CrontabField(CrontabFieldKind.Hour),
            new CrontabField(CrontabFieldKind.DayOfMonth),
            new CrontabField(CrontabFieldKind.Month),
            new CrontabField(CrontabFieldKind.DayOfWeek)
        };

        /// <summary>
        /// Check whether the given expression can be parsed by a Crontab expression.
        /// If the expression is invalid, an ArgumentException will be thrown.
        /// </summary>
        /// <param name="expression">The expression to parse.</param>
        /// <returns>A parsed CrontabExpression.</returns>
        public static CrontabExpression Parse(string expression)
        {
            CrontabExpression crontabExpression = new CrontabExpression();
            if (crontabExpression.TryParse(expression, out string message) == false)
            {
                throw new ArgumentException($"Crontab expression: \"{expression}\" is invalid. " + message);
            }

            return crontabExpression;
        }

        /// <summary>
        /// Check whether the given expression can be parsed by the Crontab expression.
        /// </summary>
        /// <param name="expression">The expression to parse.</param>
        /// <param name="message">The error message.</param>
        /// <returns>True if the expression is valid and parsed successfully, otherwise false.</returns>
        private bool TryParse(string expression, out string message)
        {
            if (expression == null)
            {
                message = $"Expression is null.";
                return false;
            }

            message = "";
            string[] fields = expression.Split(WhitespaceDelimiters, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length != NumberOfFields)
            {
                message = $"Five fields in the sequence of Minute, Hour, Day of month, Month, and Day of week are required.";
                return false;
            }

            for (int i = 0; i < NumberOfFields; i++)
            {
                if (CrontabFields[i].TryParse(fields[i], out message) == false)
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
            /*
            If month, day of month, and day of week are all asterisk characters, every day shall be matched.
            
            If either the month or day of month is specified as an element or list, but the day of week is an asterisk, 
            the month and day of month fields shall specify the days that match. 

            If both month and day of month are specified as an <asterisk>, but day of week is an element or list, 
            then only the specified days of the week match. 

            If either the month or day of month is specified as an element or list, and the day of week is also specified as an element or list, 
            then any day matching either the month and day of month, or the day of week, shall be matched.
             */
            bool isDayMatched;
            if (!CrontabFields[4].IsAsterisk && // the day of week is specified 
                (!CrontabFields[3].IsAsterisk || !CrontabFields[2].IsAsterisk)) // either the month or day of month is specified
            {
                isDayMatched = CrontabFields[4].Match((int)time.DayOfWeek)
                            || (CrontabFields[3].Match((int)time.Month) && CrontabFields[2].Match((int)time.Day));
            }
            else
            {
                isDayMatched = CrontabFields[4].Match((int)time.DayOfWeek)
                            && CrontabFields[3].Match((int)time.Month) 
                            && CrontabFields[2].Match((int)time.Day);
            }

            bool isTimeSpanMatched = CrontabFields[1].Match((int)time.Hour) && CrontabFields[0].Match((int)time.Minute);

            bool isMatched = isDayMatched && isTimeSpanMatched;

            return isMatched;
        }
    }
}
