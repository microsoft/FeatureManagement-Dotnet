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
        private readonly CronFieldKind _kind;
        private readonly BitArray _bits;

        /// <summary>
        /// Initialize the BitArray.
        /// </summary>
        /// <param name="kind">The CronField kind. <see cref="CronFieldKind"/></param>
        public CronField(CronFieldKind kind)
        {
            _kind = kind;

            (int minValue, int maxValue) = GetFieldRange(kind);

            _bits = new BitArray(maxValue - minValue + 1);
        }

        /// <summary>
        /// Whether the CronField is an asterisk.
        /// </summary>
        public bool MatchesAll { get; private set; }

        /// <summary>
        /// Checks whether the field matches the give value.
        /// </summary>
        /// <param name="value">The value to match.</param>
        /// <returns>True if the value is matched, otherwise false.</returns>
        public bool Match(int value)
        {
            if (!IsValueValid(_kind, value))
            {
                return false;
            }
            else
            {
                if (MatchesAll)
                {
                    return true;
                }

                if (_kind == CronFieldKind.DayOfWeek && value == 0) // Corner case for Sunday: both 0 and 7 can be interpreted to Sunday
                {
                    return _bits[0] | _bits[7];
                }

                return _bits[ValueToIndex(_kind, value)];
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

            if (content == null)
            {
                return false;
            }

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

                if (TryGetNumber(kind, segment, out int value) == true)
                {
                    int temp = ValueToIndex(kind, value);

                    cronField._bits[temp] = true;

                    continue;
                }

                if (segment.Contains("-") || segment.Contains("*")) // The segment might be a range.
                {
                    if (string.Equals(segment, "*"))
                    {
                        cronField.MatchesAll = true;
                    }

                    string[] parts = segment.Split('/');

                    int step = 1;

                    if (parts.Length > 2) // multiple slashs
                    {
                        return false;
                    }
                    
                    if (parts.Length == 2) 
                    { 
                        if (int.TryParse(parts[1], out step) == false || step <= 0)
                        {
                            return false;
                        }
                    }

                    string range = parts[0];

                    int first, last;

                    if (string.Equals(range, "*")) // asterisk represents unrestricted range
                    {
                        (first, last) = GetFieldRange(kind);
                    }
                    else // range should be defined by two numbers separated with a hyphen
                    {
                        string[] numbers = range.Split('-');

                        if (numbers.Length != 2)
                        {
                            return false;
                        }

                        if (TryGetNumber(kind, numbers[0], out first) == false || TryGetNumber(kind, numbers[1], out last) == false)
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
                        cronField._bits[ValueToIndex(kind, num)] = true;
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

        private static (int, int) GetFieldRange(CronFieldKind kind)
        {
            (int minValue, int maxValue) = kind switch
            {
                CronFieldKind.Minute => (0, 59),
                CronFieldKind.Hour => (0, 23),
                CronFieldKind.DayOfMonth => (1, 31),
                CronFieldKind.Month => (1, 12),
                CronFieldKind.DayOfWeek => (0, 7),
                _ => throw new ArgumentException("Invalid Cron field kind.", nameof(kind))
            };

            return (minValue, maxValue);
        }

        private static bool TryGetNumber(CronFieldKind kind, string str, out int result)
        {
            if (str == null)
            {
                result = -1;

                return false;
            }

            if (int.TryParse(str, out result) == true)
            {
                return IsValueValid(kind, result);
            }
            else
            {
                if (kind == CronFieldKind.Month)
                {
                    return TryGetMonthNumber(str, out result);
                }
                else if (kind == CronFieldKind.DayOfWeek)
                {
                    return TryGetDayOfWeekNumber(str, out result);
                }
                else
                {
                    result = -1;

                    return false;
                }
            }
        }

        private static bool TryGetMonthNumber(string name, out int result)
        {
            if (name == null)
            {
                result = -1;

                return false;
            }

            result = name.ToUpper() switch
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

            if (result == -1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private static bool TryGetDayOfWeekNumber(string name, out int result)
        {
            if (name == null)
            {
                result = -1;

                return false;
            }

            result = name.ToUpper() switch
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

            if (result == -1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private static bool IsValueValid(CronFieldKind kind, int value)
        {
            (int minValue, int maxValue) = GetFieldRange(kind);

            return (value >= minValue) && (value <= maxValue);
        }

        private static int ValueToIndex(CronFieldKind kind, int value)
        {
            string ValueOutOfRangeErrorMessage = $"Value is out of the range of {kind} field.";

            if (!IsValueValid(kind, value))
            {
                throw new ArgumentException(ValueOutOfRangeErrorMessage, nameof(value));
            }

            (int minValue, int _) = GetFieldRange(kind);

            return value - minValue;
        }
    }
}
