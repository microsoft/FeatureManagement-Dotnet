// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections;

namespace Microsoft.FeatureManagement.FeatureFilters.Cron
{
    /// <summary>
    /// The CronField uses a BitArray to record which values are included in the field.
    /// </summary>
    internal class CronField
    {
        /// <summary>
        /// Whether the CronField is an asterisk.
        /// </summary>
        public bool MatchAll { get; private set; } = false;

        private readonly CronFieldKind _kind;
        private readonly int _minValue = -1;
        private readonly int _maxValue = -1;
        private readonly BitArray _bits;

        /// <summary>
        /// Set the min value and max value according to the field kind.
        /// Initialize the BitArray.
        /// </summary>
        public CronField(CronFieldKind kind)
        {
            _kind = kind;

            (_minValue, _maxValue) = kind switch
            {
                CronFieldKind.Minute => (0, 59),
                CronFieldKind.Hour => (0, 23),
                CronFieldKind.DayOfMonth => (1, 31),
                CronFieldKind.Month => (1, 12),
                CronFieldKind.DayOfWeek => (0, 7),
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
                if (MatchAll)
                {
                    return true;
                }

                if (_kind == CronFieldKind.DayOfWeek && value == 0) // Corner case for Sunday: both 0 and 7 can be interpreted to Sunday
                {
                    return _bits[0] | _bits[7];
                }

                return _bits[ValueToIndex(value)];
            }
        }

        /// <summary>
        /// Checks whether the given content can be fit into the CronField.
        /// </summary>
        /// <param name="kind">The CronField kind. <see cref="CronFieldKind"/></param>
        /// <param name="content">The content to parse.</param>
        /// <param name="result">The parsed result.</param>
        /// <returns>True if the content is valid and parsed successfully, otherwise false.</returns>
        public static bool TryParse(CronFieldKind kind, string content, out CronField result)
        {
            result = null;
            CronField cronField = new CronField(kind);

            //
            // The field can be a list which is a set of numbers or ranges separated by commas.
            // Ranges are two numbers/names separated with a hyphen or an asterisk which represents all possible values in the field.
            // Step values can be used in conjunction with ranges after a slash.
            string[] segments = content.Split(',');
            foreach (string segment in segments)
            {
                if (segment == null)
                {
                    return false;
                }

                int value = cronField.TryGetNumber(segment);
                if (cronField.IsValueValid(value))
                {
                    cronField._bits[cronField.ValueToIndex(value)] = true;
                    continue;
                }

                if (segment.Contains("-") || segment.Contains("*")) // The segment might be a range.
                {
                    if (string.Equals(segment, "*"))
                    {
                        cronField.MatchAll = true;
                    }

                    string[] parts = segment.Split('/');
                    if (parts.Length > 2) // multiple slashs
                    {
                        return false;
                    }

                    int step = parts.Length == 2 ? cronField.TryGetNumber(parts[1]) : 1;
                    if (step <= 0) // invalid step value 
                    {
                        return false;
                    }

                    string range = parts[0];
                    int first, last;
                    if (string.Equals(range, "*")) // asterisk represents unrestricted range
                    {
                        (first, last) = (cronField._minValue, cronField._maxValue);
                    }
                    else // range should be defined by two numbers separated with a hyphen
                    {
                        string[] numbers = range.Split('-');
                        if (numbers.Length != 2)
                        {
                            return false;
                        }

                        (first, last) = (cronField.TryGetNumber(numbers[0]), cronField.TryGetNumber(numbers[1]));

                        if (!cronField.IsValueValid(first) || !cronField.IsValueValid(last))
                        {
                            return false;
                        }

                        if (cronField._kind == CronFieldKind.DayOfWeek && last == 0 && last != first) // Corner case for Sunday: both 0 and 7 can be interpreted to Sunday
                        {
                            last = 7; // Mon-Sun should be intepreted to 1-7 instead of 1-0
                        }

                        if (first > last)
                        {
                            return false;
                        }
                    }

                    for (int num = first; num <= last; num += step)
                    {
                        cronField._bits[cronField.ValueToIndex(num)] = true;
                    }
                }
                else // The segment is neither a range nor a valid number.
                {
                    return false;
                }
            }

            result = cronField;
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
                if (_kind == CronFieldKind.Month)
                {
                    return TryGetMonthNumber(str);
                }
                else if (_kind == CronFieldKind.DayOfWeek)
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
    }
}
