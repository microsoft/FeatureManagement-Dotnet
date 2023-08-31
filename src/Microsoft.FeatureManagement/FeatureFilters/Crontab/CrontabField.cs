// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace Microsoft.FeatureManagement.FeatureFilters.Crontab
{

    /// <summary>
    /// The Crontab field uses a BitArray to record which values are included in the field.
    /// </summary>
    public class CrontabField
    {
        private readonly CrontabFieldKind _kind;
        private readonly int _minValue = -1;
        private readonly int _maxValue = -1;
        private readonly BitArray _bits;

        /// <summary>
        /// Set the min value and max value according to the field kind.
        /// Initialize the BitArray.
        /// </summary>
        public CrontabField(CrontabFieldKind kind)
        {
            _kind = kind;

            (_minValue, _maxValue) = kind switch
            {
                CrontabFieldKind.Minute => (0, 59),
                CrontabFieldKind.Hour => (0, 23),
                CrontabFieldKind.DayOfMonth => (1, 31),
                CrontabFieldKind.Month => (1, 12),
                CrontabFieldKind.DayOfWeek => (0, 7),
                _ => throw new ArgumentException("Invalid crontab field kind.")
            };

            _bits = new BitArray(_maxValue - _minValue + 1);
        }

        /// <summary>
        /// Checks whether the field matches the give value.
        /// </summary>
        /// <param name="value">The value to match.</param>
        /// <returns>True if the value is matched, otherwise false.</returns>
        public bool Match(int value)
        {
            if (!IsValueValid(value))
            {
                return false;
            }
            else
            {
                if (_kind == CrontabFieldKind.DayOfWeek && value == 0) // Corner case for Sunday: both 0 and 7 can be interpreted to Sunday
                {
                    return _bits[0] | _bits[7];
                }

                return _bits[ValueToIndex(value)];
            }
        }

        /// <summary>
        /// Checks whether the given content can be parsed by the Crontab field.
        /// </summary>
        /// <param name="content">The content to parse.</param>
        /// <param name="message">The error message.</param>
        /// <returns>True if the content is valid and parsed successfully, otherwise false.</returns>
        public bool Parse(string content, out string message)
        {
            message = "";
            _bits.SetAll(false);

            //
            // The field can be a list which is a set of numbers or ranges separated by commas.
            // Ranges are two numbers/name separated with a hyphen or an asterisk which represents all possible values in the field.
            // Step values can be used in conjunction with ranges after a slash.
            string validSegmentPattern = @"^(?:[0-9]+|[a-zA-Z]{3}|(?:\*|(?:[0-9]+|[a-zA-Z]{3})-(?:[0-9]+|[a-zA-Z]{3}))(/[0-9]+)?)$";
            string[] segments = content.Split(',');
            foreach (string segment in segments)
            {
                if (Regex.IsMatch(segment, validSegmentPattern))
                {
                    string noRangePattern = @"^(?:[0-9]+|[a-zA-Z]{3})$";
                    if (Regex.IsMatch(segment, noRangePattern))
                    {
                        int num = GetNumber(segment);
                        if (!IsValueValid(num))
                        {
                            message = $"Content of the {_kind} field: {content} is invalid. Value {segment} is out of range [{_minValue}, {_maxValue}].";
                            return false;
                        }
                        else
                        {
                            _bits[ValueToIndex(num)] = true;
                        }
                    }
                    else // The segment is a range.
                    {
                        string[] parts = segment.Split('/');
                        string range = parts[0];
                        int step = parts.Length > 1 ? GetNumber(parts[1]) : 1;

                        int first, last;
                        if (string.Equals(range, "*"))
                        {
                            (first, last) = (_minValue, _maxValue);

                            if (_kind == CrontabFieldKind.DayOfWeek) // Corner case for Sunday: both 0 and 7 can be interpreted to Sunday
                            {
                                last = _maxValue - 1;
                            }
                        }
                        else
                        {
                            string[] numbers = range.Split('-');
                            (first, last) = (GetNumber(numbers[0]), GetNumber(numbers[1]));

                            if (!IsValueValid(first) || !IsValueValid(last))
                            {
                                message = $"Content of the {_kind} field: {content} is invalid. Value {segment} is out of range [{_minValue}, {_maxValue}].";
                                return false;
                            }
                        }

                        if (_kind == CrontabFieldKind.DayOfWeek && last == 0 && last != first) // Corner case for Sunday: both 0 and 7 can be interpreted to Sunday
                        {
                            last = 7; // Mon-Sun should be intepreted to 1-7 instead of 1-0
                        }

                        for (int num = first; num <= last; num += step)
                        {
                            _bits[ValueToIndex(num)] = true;
                        }
                    }
                }
                else // The segment is not a valid number or range.
                {
                    message = $"Content of the {_kind} field: {content} is invalid. Syntax cannot be parsed.";
                    return false;
                }
            }

            return true;
        }

        private int GetNumber(string str)
        {
            if (int.TryParse(str, out int num) == true)
            {
                return num;
            }
            else
            {
                if (_kind == CrontabFieldKind.Month)
                {
                    return GetMonthNumber(str);
                }
                else if (_kind == CrontabFieldKind.DayOfWeek)
                {
                    return GetDayOfWeekNumber(str);
                }
                else
                {
                    return -1;
                }
            }
        }

        static private int GetMonthNumber(string name)
        {
            return name.ToUpper() switch
            {
                "JAN" => 1,
                "FEB" => 2,
                "MAR" => 3,
                "APR" => 4,
                "MAY" => 5,
                "JUN" => 6,
                "JUL" => 7,
                "AUG" => 8,
                "SEP" => 9,
                "OCT" => 10,
                "NOV" => 11,
                "DEC" => 12,
                _ => -1
            };
        }

        static private int GetDayOfWeekNumber(string name)
        {
            return name.ToUpper() switch
            {
                "SUN" => 0,
                "MON" => 1,
                "TUE" => 2,
                "WED" => 3,
                "THU" => 4,
                "FRI" => 5,
                "SAT" => 6,
                _ => -1
            };
        }

        private bool IsValueValid(int value)
        {
            return (value >= _minValue) && (value <= _maxValue);
        }

        private int ValueToIndex(int value)
        {
            return value - _minValue;
        }
    }
}
