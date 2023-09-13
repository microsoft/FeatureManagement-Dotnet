// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections;

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
        /// Whether the CrontabField is an asterisk.
        /// </summary>
        public bool IsAsterisk { get; private set; } = false;

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
                CrontabFieldKind.DayOfWeek => (0, 6),
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
                if (IsAsterisk)
                {
                    return true;
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
        public bool TryParse(string content, out string message)
        {
            message = "";
            _bits.SetAll(false);

            //
            // The field can be a list which is a set of numbers or ranges separated by commas.
            // Ranges are two numbers/names separated with a hyphen or an asterisk which represents all possible values in the field.
            // Step values can be used in conjunction with ranges after a slash.
            if (string.Equals(content, "*"))
            {
                IsAsterisk = true;
            }
            
            string[] segments = content.Split(',');
            foreach (string segment in segments)
            {
                if (segment == null)
                {
                    message = InvalidSyntaxErrorMessage(content);
                    return false;
                }

                int value = TryGetNumber(segment);
                if (IsValueValid(value))
                {
                    _bits[ValueToIndex(value)] = true;
                    continue;
                }

                if (segment.Contains("-") || segment.Contains("*")) // The segment might be a range.
                {
                    string[] parts = segment.Split('/');
                    if (parts.Length > 2) // multiple slashs
                    {
                        message = InvalidSyntaxErrorMessage(content);
                        return false;
                    }

                    int step = parts.Length == 2 ? TryGetNumber(parts[1]) : 1;
                    if (step <= 0) // invalid step value 
                    {
                        message = InvalidValueErrorMessage(content, parts[1]);
                        return false;
                    }

                    string range = parts[0];
                    int first, last;
                    if (string.Equals(range, "*")) // asterisk represents unrestricted range
                    {
                        (first, last) = (_minValue, _maxValue);
                    }
                    else // range should be defined by two numbers separated with a hyphen
                    {
                        string[] numbers = range.Split('-');
                        if (numbers.Length != 2)
                        {
                            message = InvalidSyntaxErrorMessage(content);
                            return false;
                        }

                        (first, last) = (TryGetNumber(numbers[0]), TryGetNumber(numbers[1]));

                        if (!IsValueValid(first) || !IsValueValid(last))
                        {
                            message = InvalidValueErrorMessage(content, range);
                            return false;
                        }

                        if (first > last)
                        {
                            message = InvalidSyntaxErrorMessage(content);
                            return false;
                        }
                    }

                    for (int num = first; num <= last; num += step)
                    {
                        _bits[ValueToIndex(num)] = true;
                    }
                }
                else // The segment is neither a range nor a valid number.
                {
                    message = InvalidValueErrorMessage(content, segment);
                    return false;
                }
            }

            return true;
        }

        private int TryGetNumber(string str)
        {
            if (int.TryParse(str, out int num) == true)
            {
                return num;
            }
            else
            {
                if (_kind == CrontabFieldKind.Month)
                {
                    return TryGetMonthNumber(str);
                }
                else if (_kind == CrontabFieldKind.DayOfWeek)
                {
                    return TryGetDayOfWeekNumber(str);
                }
                else
                {
                    return -1;
                }
            }
        }

        private static int TryGetMonthNumber(string name)
        {
            if (name == null) return -1;

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

        private static int TryGetDayOfWeekNumber(string name)
        {
            if (name == null) return -1;

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

        private string InvalidSyntaxErrorMessage(string content)
        {
            return $"Content of the {_kind} field: {content} is invalid. Syntax cannot be parsed.";
        }

        private string InvalidValueErrorMessage(string content, string segment)
        {
            return $"Content of the {_kind} field: {content} is invalid. The value of {segment} is invalid.";
        }
    }
}
