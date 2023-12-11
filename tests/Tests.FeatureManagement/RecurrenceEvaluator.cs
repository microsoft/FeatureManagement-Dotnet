// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;
using Xunit;

namespace Tests.FeatureManagement
{
    class ErrorMessage
    {
        public const string OutOfRange = "The value is out of the accepted range.";
        public const string UnrecognizableValue = "The value is unrecognizable.";
        public const string RequiredParameter = "Value cannot be null.";
        public const string NotMatched = "Start date is not a valid first occurrence.";
    }

    class ParamName
    {
        public const string Start = "Start";
        public const string End = "End";

        public const string Pattern = "Recurrence.Pattern";
        public const string PatternType = "Recurrence.Pattern.Type";
        public const string Interval = "Recurrence.Pattern.Interval";
        public const string Index = "Recurrence.Pattern.Index";
        public const string DaysOfWeek = "Recurrence.Pattern.DaysOfWeek";
        public const string FirstDayOfWeek = "Recurrence.Pattern.FirstDayOfWeek";
        public const string Month = "Recurrence.Pattern.Month";
        public const string DayOfMonth = "Recurrence.Pattern.DayOfMonth";

        public const string Range = "Recurrence.Range";
        public const string RangeType = "Recurrence.Range.Type";
        public const string NumberOfOccurrences = "Recurrence.Range.NumberOfOccurrences";
        public const string RecurrenceTimeZone = "Recurrence.Range.RecurrenceTimeZone";
        public const string EndDate = "Recurrence.Range.EndDate";
    }

    public class RecurrenceEvaluatorTest
    {
        private static void ConsumeValidationTestData(List<ValueTuple<TimeWindowFilterSettings, string, string>> testData)
        {
            foreach ((TimeWindowFilterSettings settings, string paramName, string errorMessage) in testData)
            {
                ArgumentException ex = Assert.Throws<ArgumentException>(
                () =>
                {
                    RecurrenceEvaluator.MatchRecurrence(DateTimeOffset.Now, settings);
                });

                Assert.Equal(paramName, ex.ParamName);
                Assert.Equal(errorMessage, ex.Message.Substring(0, errorMessage.Length));
            }
        }

        private static void ConsumeEvalutationTestData(List<ValueTuple<DateTimeOffset, TimeWindowFilterSettings, bool>> testData)
        {
            foreach ((DateTimeOffset time, TimeWindowFilterSettings settings, bool expected) in testData)
            {
                Assert.Equal(RecurrenceEvaluator.MatchRecurrence(time, settings), expected);
            }
        }

        [Fact]
        public void GeneralRequiredParameterTest()
        {
            var testData = new List<ValueTuple<TimeWindowFilterSettings, string, string>>()
            {
                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-25T12:00:00+08:00"),
                    End = null,
                    Recurrence = new Recurrence()
                },
                ParamName.End,
                ErrorMessage.RequiredParameter ),

                ( new TimeWindowFilterSettings()
                {
                    Start = null,
                    End = DateTimeOffset.Parse("2023-9-25T12:00:00+08:00"),
                    Recurrence = new Recurrence()
                },
                ParamName.Start,
                ErrorMessage.RequiredParameter ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-25T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-25T02:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = null,
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.Pattern,
                ErrorMessage.RequiredParameter ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-25T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-25T02:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern(),
                        Range = null
                    }
                },
                ParamName.Range,
                ErrorMessage.RequiredParameter ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-25T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-25T02:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = null
                        },
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.PatternType,
                ErrorMessage.RequiredParameter ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-25T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-25T02:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily",
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = null
                        }
                    }
                },
                ParamName.RangeType,
                ErrorMessage.RequiredParameter ),
            };

            ConsumeValidationTestData(testData);
        }

        [Fact]
        public void InvalidValueTest()
        {
            var testData = new List<ValueTuple<TimeWindowFilterSettings, string, string>>()
            {
                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = ""
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.PatternType,
                ErrorMessage.UnrecognizableValue ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = ""
                        }
                    }
                },
                ParamName.RangeType,
                ErrorMessage.UnrecognizableValue ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily",
                            Interval = 0
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.Interval,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            DaysOfWeek = new List<string>(){ "Monday" },
                            FirstDayOfWeek = ""
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.FirstDayOfWeek,
                ErrorMessage.UnrecognizableValue ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            DaysOfWeek = new List<string>(){ "day" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.DaysOfWeek,
                ErrorMessage.UnrecognizableValue ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            Index = "",
                            DaysOfWeek = new List<string>(){ "Friday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.Index,
                ErrorMessage.UnrecognizableValue ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteMonthly",
                            DayOfMonth = 0,
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.DayOfMonth,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteYearly",
                            DayOfMonth = 1,
                            Month = 0
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.Month,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd",
                            RecurrenceTimeZone = ""
                        }
                    }
                },
                ParamName.RecurrenceTimeZone,
                ErrorMessage.UnrecognizableValue ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "Numbered",
                            NumberOfOccurrences = 0
                        }
                    }
                },
                ParamName.NumberOfOccurrences,
                ErrorMessage.OutOfRange )
            };

            ConsumeValidationTestData(testData);
        }

        [Fact]
        public void InvalidTimeWindowTest()
        {
            var testData = new List<ValueTuple<TimeWindowFilterSettings, string, string>>()
            {
                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-25T12:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-25T12:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-25T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-27T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily",
                            Interval = 2
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-8T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            Interval = 1,
                            DaysOfWeek = new List<string>(){ "Friday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-5T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            Interval = 1,
                            DaysOfWeek = new List<string>(){ "Monday", "Thursday", "Sunday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-2-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-3-29T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteMonthly",
                            Interval = 2,
                            DayOfMonth = 1
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-29T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            Interval = 1,
                            DaysOfWeek = new List<string>(){ "Friday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2024-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteYearly",
                            Interval = 1,
                            DayOfMonth = 1,
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2024-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeYearly",
                            Interval = 1,
                            DaysOfWeek = new List<string>(){ "Friday" },
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "EndDate",
                            EndDate = DateTimeOffset.Parse("2023-8-31T00:00:00+08:00")
                        }
                    }
                },
                ParamName.EndDate,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T23:00:00+00:00"),
                    End = DateTimeOffset.Parse("2023-9-1T23:00:01+00:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "EndDate",
                            EndDate = DateTimeOffset.Parse("2023-9-1"),
                            RecurrenceTimeZone = "UTC+08:00"
                        }
                    }
                },
                ParamName.EndDate,
                ErrorMessage.OutOfRange )
            };

            ConsumeValidationTestData(testData);
        }

        [Fact]
        public void WeeklyPatternRequiredParameterTest()
        {
            var testData = new List<ValueTuple<TimeWindowFilterSettings, string, string>>()
            {
                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            DaysOfWeek = null
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.DaysOfWeek,
                ErrorMessage.RequiredParameter )
            };

            ConsumeValidationTestData(testData);
        }

        [Fact]
        public void WeeklyPatternNotMatchTest()
        {
            var testData = new List<ValueTuple<TimeWindowFilterSettings, string, string>>()
            {
                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            DaysOfWeek = new List<string>{ "Monday", "Tuesday", "Wednesday", "Thursday", "Saturday", "Sunday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.Start,
                ErrorMessage.NotMatched ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            DaysOfWeek = new List<string>{ "Friday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd",
                            RecurrenceTimeZone = "UTC+07:00"
                        }
                    }
                },
                ParamName.Start,
                ErrorMessage.NotMatched )
            };

            ConsumeValidationTestData(testData);
        }

        [Fact]
        public void AbsoluteMonthlyPatternRequiredParameterTest()
        {
            var testData = new List<ValueTuple<TimeWindowFilterSettings, string, string>>()
            {
                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteMonthly",
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.DayOfMonth,
                ErrorMessage.RequiredParameter )
            };

            ConsumeValidationTestData(testData);
        }

        [Fact]
        public void AbsoluteMonthlyPatternNotMatchTest()
        {
            var testData = new List<ValueTuple<TimeWindowFilterSettings, string, string>>()
            {
                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-2T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-2T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteMonthly",
                            DayOfMonth = 1
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd",
                        }
                    }
                },
                ParamName.Start,
                ErrorMessage.NotMatched )
            };

            ConsumeValidationTestData(testData);
        }

        [Fact]
        public void RelativeMonthlyPatternRequiredParameterTest()
        {
            var testData = new List<ValueTuple<TimeWindowFilterSettings, string, string>>()
            {
                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>()
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.DaysOfWeek,
                ErrorMessage.RequiredParameter ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>(){ "Friday" },
                            Index = null
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.Index,
                ErrorMessage.RequiredParameter )
            };

            ConsumeValidationTestData(testData);
        }

        [Fact]
        public void RelativeMonthlyPatternNotMatchTest()
        {
            var testData = new List<ValueTuple<TimeWindowFilterSettings, string, string>>()
            {
                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>{ "Friday" },
                            Index = "Second"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.Start,
                ErrorMessage.NotMatched )
            };

            ConsumeValidationTestData(testData);
        }

        [Fact]
        public void AbsoluteYearlyPatternRequiredParameterTest()
        {
            var testData = new List<ValueTuple<TimeWindowFilterSettings, string, string>>()
            {
                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteYearly",
                            DayOfMonth = 1
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.Month,
                ErrorMessage.RequiredParameter ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteYearly",
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.DayOfMonth,
                ErrorMessage.RequiredParameter )
            };

            ConsumeValidationTestData(testData);
        }

        [Fact]
        public void AbsoluteYearlyPatternNotMatchTest()
        {
            var testData = new List<ValueTuple<TimeWindowFilterSettings, string, string>>()
            {
                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteYearly",
                            DayOfMonth = 1,
                            Month = 8
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd",
                        }
                    }
                },
                ParamName.Start,
                ErrorMessage.NotMatched )
            };

            ConsumeValidationTestData(testData);
        }

        [Fact]
        public void RelativeYearlyPatternRequiredParameterTest()
        {
            var testData = new List<ValueTuple<TimeWindowFilterSettings, string, string>>()
            {
                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeYearly",
                            DaysOfWeek = null,
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.DaysOfWeek,
                ErrorMessage.RequiredParameter ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeYearly",
                            DaysOfWeek = new List<string>{ "Friday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.Month,
                ErrorMessage.RequiredParameter ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeYearly",
                            DaysOfWeek = new List<string>{ "Friday" },
                            Month = 9,
                            Index = null
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.Index,
                ErrorMessage.RequiredParameter )
            };

            ConsumeValidationTestData(testData);
        }

        [Fact]
        public void RelativeYearlyPatternNotMatchTest()
        {
            var testData = new List<ValueTuple<TimeWindowFilterSettings, string, string>>()
            {
                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeYearly",
                            DaysOfWeek = new List<string>{ "Friday" },
                            Month = 8
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.Start,
                ErrorMessage.NotMatched ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeYearly",
                            DaysOfWeek = new List<string>{ "Friday" },
                            Month = 9,
                            Index = "Second"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                ParamName.Start,
                ErrorMessage.NotMatched )
            };

            ConsumeValidationTestData(testData);
        }

        [Fact]
        public void MatchDailyRecurrenceTest()
        {
            var testData = new List<ValueTuple<DateTimeOffset, TimeWindowFilterSettings, bool>>()
            {
                ( DateTimeOffset.Parse("2023-9-2T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-2T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily",
                            Interval = 2
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-5T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily",
                            Interval = 4
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-6T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily",
                            Interval = 4
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-9T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily",
                            Interval = 4
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily",
                            Interval = 2
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "Numbered",
                            NumberOfOccurrences = 2
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-2T17:00:00+00:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T17:00:00+00:00"),
                    End = DateTimeOffset.Parse("2023-9-1T17:30:00+00:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Daily"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "EndDate",
                            EndDate = DateTimeOffset.Parse("2023-9-2"),
                            RecurrenceTimeZone = "UTC+08:00"
                        }
                    }
                },
                false )
            };

            ConsumeEvalutationTestData(testData);
        }

        [Fact]
        public void MatchWeeklyRecurrenceTest()
        {
            var testData = new List<ValueTuple<DateTimeOffset, TimeWindowFilterSettings, bool>>()
            {
                ( DateTimeOffset.Parse("2023-9-4T00:00:00+08:00"), // Monday in the 2nd week
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Friday
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            DaysOfWeek = new List<string>(){ "Monday", "Friday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"), // Friday in the 2nd week
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Friday
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            Interval = 2,
                            DaysOfWeek = new List<string>(){ "Friday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-15T00:00:00+08:00"), // Friday in the 3rd week
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Friday
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            Interval = 2,
                            DaysOfWeek = new List<string>(){ "Friday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-4T00:00:00+08:00"), // Monday in the 2nd week
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Sunday
                    End = DateTimeOffset.Parse("2023-9-3T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            Interval = 2,
                            FirstDayOfWeek = "Monday",
                            DaysOfWeek = new List<string>(){ "Monday", "Sunday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-4T00:00:00+08:00"), // Monday in the 1st week
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Sunday
                    End = DateTimeOffset.Parse("2023-9-3T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            Interval = 2,
                            FirstDayOfWeek = "Sunday",
                            DaysOfWeek = new List<string>(){ "Monday", "Sunday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-4T00:00:00+08:00"), // Monday in the 2nd week
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Friday
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            DaysOfWeek = new List<string>(){ "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-2T00:00:00+08:00"), // Saturday in the 1st week
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Friday
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            DaysOfWeek = new List<string>(){ "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "Numbered",
                            NumberOfOccurrences = 1
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-18T00:00:00+08:00"), // Monday in the 4th week
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Sunday
                    End = DateTimeOffset.Parse("2023-9-3T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            Interval = 2,
                            FirstDayOfWeek = "Monday",
                            DaysOfWeek = new List<string>(){ "Monday", "Sunday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-18T00:00:00+08:00"), // Monday in the 3rd week
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Sunday
                    End = DateTimeOffset.Parse("2023-9-3T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            Interval = 2,
                            FirstDayOfWeek = "Sunday",
                            DaysOfWeek = new List<string>(){ "Monday", "Sunday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-17T00:00:00+08:00"), // Sunday in the 3rd week
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Sunday
                    End = DateTimeOffset.Parse("2023-9-3T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            Interval = 2,
                            FirstDayOfWeek = "Monday",
                            DaysOfWeek = new List<string>(){ "Monday", "Sunday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "Numbered",
                            NumberOfOccurrences = 3
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-13T00:00:00+08:00"), // Wednesday in the 3rd week
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Sunday
                    End = DateTimeOffset.Parse("2023-9-7T00:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            Interval = 2,
                            FirstDayOfWeek = "Monday",
                            DaysOfWeek = new List<string>(){ "Monday", "Sunday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-19T00:00:00+08:00"), // Tuesday in the 4th week
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Sunday
                    End = DateTimeOffset.Parse("2023-9-7T00:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            Interval = 2,
                            FirstDayOfWeek = "Monday",
                            DaysOfWeek = new List<string>(){ "Monday", "Sunday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "Numbered",
                            NumberOfOccurrences = 3
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-19T00:00:00+08:00"), // Tuesday in the 4th week
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Sunday
                    End = DateTimeOffset.Parse("2023-9-7T00:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "Weekly",
                            Interval = 2,
                            FirstDayOfWeek = "Monday",
                            DaysOfWeek = new List<string>(){ "Monday", "Sunday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "Numbered",
                            NumberOfOccurrences = 2
                        }
                    }
                },
                false )
            };

            ConsumeEvalutationTestData(testData);
        }

        [Fact]
        public void MatchAbsoluteMonthlyRecurrenceTest()
        {
            var testData = new List<ValueTuple<DateTimeOffset, TimeWindowFilterSettings, bool>>()
            {
                ( DateTimeOffset.Parse("2023-10-1T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteMonthly",
                            DayOfMonth = 1
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2024-2-1T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteMonthly",
                            DayOfMonth = 1,
                            Interval = 5
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2024-1-1T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteMonthly",
                            DayOfMonth = 1,
                            Interval = 5
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2024-9-1T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteMonthly",
                            DayOfMonth = 1,
                            Interval = 4
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "Numbered",
                            NumberOfOccurrences = 3
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2024-2-29T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-4-29T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-4-29T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteMonthly",
                            DayOfMonth = 29,
                            Interval = 2
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2024-2-29T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-4-29T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-4-29T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteMonthly",
                            DayOfMonth = 29,
                            Interval = 2
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "EndDate",
                            EndDate = DateTimeOffset.Parse("2024-2-29")
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-10-29T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-29T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-29T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteMonthly",
                            DayOfMonth = 29,
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "EndDate",
                            EndDate = DateTimeOffset.Parse("2023-10-28")
                        }
                    }
                },
                false )
            };

            ConsumeEvalutationTestData(testData);
        }

        [Fact]
        public void MatchRelativeMonthlyRecurrenceTest()
        {
            var testData = new List<ValueTuple<DateTimeOffset, TimeWindowFilterSettings, bool>>()
            {
                ( DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"), // 2nd Friday in 2023 Sep 
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-10-13T00:00:00+08:00"), // 2nd Friday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-8T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday" },
                            Index = "Second"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-10-13T00:00:00+08:00"), // 2nd Friday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-8T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday" },
                            Index = "Second",
                            Interval = 3
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-12-8T00:00:00+08:00"), // 2nd Friday in 2023 Dec
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-8T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday" },
                            Index = "Second",
                            Interval = 3
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-15T00:00:00+08:00"), // 3rd Friday in 2023 Sep
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-8T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday" },
                            Index = "Second"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-10-6T00:00:00+08:00"), // 1st Friday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday" },
                            Index = "First"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-10-27T00:00:00+08:00"), // 4th Friday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-29T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-29T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday" },
                            Index = "Last"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-11-24T00:00:00+08:00"), // 4th Friday in 2023 Nov
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-29T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-29T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday" },
                            Index = "Last"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-12-29T00:00:00+08:00"), // 5th Friday in 2023 Dec
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-29T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-29T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday" },
                            Index = "Last"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-10-29T00:00:00+08:00"), // 5th Sunday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-25T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-25T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Sunday", "Monday" },
                            Index = "Last"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-10-30T00:00:00+08:00"), // 5th Monday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-25T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-25T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Sunday", "Monday" },
                            Index = "Last"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-10-6T00:00:00+08:00"), // 1st Friday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday" },
                            Index = "First",
                            Interval = 3
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-12-1T00:00:00+08:00"), // 1st Friday in 2023 Dec
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday" },
                            Interval = 3
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-10-2T00:00:00+08:00"), // 1st Monday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday", "Monday" },
                            Index = "First"
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-10-6T00:00:00+08:00"), // 1st Friday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday", "Monday" }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-10-2T00:00:00+08:00"), // 1st Monday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday", "Monday" },
                            Index = "First",
                            Interval = 2
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-11-3T00:00:00+08:00"), // 1st Friday in 2023 Nov
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday", "Monday" },
                            Index = "First",
                            Interval = 2
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-11-6T00:00:00+08:00"), // 1st Monday in 2023 Nov
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Friday", "Monday" },
                            Index = "First",
                            Interval = 2
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-12-1T00:00:00+08:00"), // the first day of the month
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" },
                            Interval = 3
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2024-3-1T00:00:00+08:00"), // the first day of the month
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" },
                            Interval = 3
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-12-1T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" },
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "Numbered",
                            NumberOfOccurrences = 3
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-10-1T00:00:00+08:00"), // Sunday
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" },
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-10-2T00:00:00+08:00"), // Monday
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" },
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-10-3T00:00:00+08:00"), // Tuesday
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeMonthly",
                            DaysOfWeek = new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Sunday" },
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false )
            };

            ConsumeEvalutationTestData(testData);
        }

        [Fact]
        public void MatchAbsoluteYearlyRecurrenceTest()
        {
            var testData = new List<ValueTuple<DateTimeOffset, TimeWindowFilterSettings, bool>>()
            {
                ( DateTimeOffset.Parse("2024-9-1T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteYearly",
                            DayOfMonth = 1,
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2024-9-1T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteYearly",
                            DayOfMonth = 1,
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "Numbered",
                            NumberOfOccurrences = 1
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2026-9-1T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteYearly",
                            DayOfMonth = 1,
                            Month = 9,
                            Interval = 3
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "Numbered",
                            NumberOfOccurrences = 2
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2029-9-1T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteYearly",
                            DayOfMonth = 1,
                            Month = 9,
                            Interval = 3
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "Numbered",
                            NumberOfOccurrences = 2
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2024-10-1T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "AbsoluteYearly",
                            DayOfMonth = 1,
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false )
            };

            ConsumeEvalutationTestData(testData);
        }

        [Fact]
        public void MatchRelativeYearlyRecurrenceTest()
        {
            var testData = new List<ValueTuple<DateTimeOffset, TimeWindowFilterSettings, bool>>()
            {
                ( DateTimeOffset.Parse("2024-9-6T00:00:00+08:00"), // 1st Friday
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeYearly",
                            DaysOfWeek = new List<string>() { "Friday" },
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2024-9-1T00:00:00+08:00"), // 1st Sunday
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeYearly",
                            DaysOfWeek = new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" },
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-2T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeYearly",
                            DaysOfWeek = new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" },
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2024-9-1T00:00:00+08:00"), // the first day of Sep
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeYearly",
                            DaysOfWeek = new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" },
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2024-10-1T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeYearly",
                            DaysOfWeek = new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" },
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2024-9-1T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeYearly",
                            DaysOfWeek = new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" },
                            Month = 9,
                            Interval = 2
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2026-9-1T00:00:00+08:00"), // the first day of Sep
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeYearly",
                            DaysOfWeek = new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" },
                            Month = 9,
                            Interval = 3
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "NoEnd"
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2026-9-1T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = "RelativeYearly",
                            DaysOfWeek = new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" },
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = "Numbered",
                            NumberOfOccurrences = 3
                        }
                    }
                },
                false ),
            };

            ConsumeEvalutationTestData(testData);
        }
    }
}
