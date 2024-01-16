// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using Xunit;
using Xunit.Sdk;

namespace Tests.FeatureManagement
{
    class ErrorMessage
    {
        public const string OutOfRange = "The value is out of the accepted range.";
        public const string UnrecognizableValue = "The value is unrecognizable.";
        public const string RequiredParameter = "Value cannot be null or empty.";
        public const string NotMatched = "Start date is not a valid first occurrence.";
    }

    class ParamName
    {
        public const string Start = "Start";
        public const string End = "End";

        public const string Pattern = "Recurrence.Pattern";
        public const string PatternType = "Recurrence.Pattern.Type";
        public const string Interval = "Recurrence.Pattern.Interval";
        public const string DaysOfWeek = "Recurrence.Pattern.DaysOfWeek";
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
                ErrorMessage.RequiredParameter )
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
                            Interval = 0 // Interval should be larger than 0.
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.AbsoluteMonthly,
                            DayOfMonth = 0, // DayOfMonth should be smaller than 32 and larger than 0.
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.AbsoluteMonthly,
                            DayOfMonth = 32, // DayOfMonth should be smaller than 32 and larger than 0.
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.AbsoluteYearly,
                            DayOfMonth = 1,
                            Month = 0 // Month should be larger than 0 and smaller than 13.
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.AbsoluteYearly,
                            DayOfMonth = 1,
                            Month = 13 // Month should be larger than 0 and smaller than 13.
                        },
                        Range = new RecurrenceRange()
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
                        Pattern = new RecurrencePattern(),
                        Range = new RecurrenceRange()
                        {
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
                        Pattern = new RecurrencePattern(),
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
                            NumberOfOccurrences = 0 // NumberOfOccurrences should be larger than 0.
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
                    End = DateTimeOffset.Parse("2023-9-25T12:00:00+08:00"), // End equals to Start.
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern(),
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-25T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-27T00:00:01+08:00"), // The duration of the time window is longer than how frequently it recurs.
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Daily,
                            Interval = 2
                        },
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-8T00:00:01+08:00"), // The duration of the time window is longer than how frequently it recurs.
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            Interval = 1,
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Friday } // 2023.9.1 is Friday.
                        },
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-5T00:00:01+08:00"), // The duration of the time window is longer than how frequently it recurs.
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            Interval = 1,
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Sunday }
                        },
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-2T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-5T00:00:01+08:00"), // The duration of the time window is longer than how frequently it recurs.
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            Interval = 1,
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Saturday } // The time window duration should be shorter than 2 days because the gap between Saturday in the previous week and Monday in this week is 2 days.
                        },
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2024-1-16T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2024-1-19T00:00:01+08:00"), // The duration of the time window is longer than how frequently it recurs.
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            Interval = 1,
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Tuesday, DayOfWeek.Saturday } // The time window duration should be shorter than 3 days because the gap between Saturday in the previous week and Tuesday in this week is 3 days.
                        },
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-2-1T00:00:00+08:00"), // The duration of the time window is longer than how frequently it recurs.
                    End = DateTimeOffset.Parse("2023-3-29T00:00:01+08:00"), // This behavior is the same as the Outlook. Outlook uses 28 days as a monthly interval.
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.AbsoluteMonthly,
                            Interval = 2,
                            DayOfMonth = 1
                        },
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // The duration of the time window is longer than how frequently it recurs.
                    End = DateTimeOffset.Parse("2023-9-29T00:00:01+08:00"), // This behavior is the same as the Outlook. Outlook uses 28 days as a monthly interval.
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            Interval = 1,
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Friday } // 2023.9.1 is Friday.
                        },
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // The duration of the time window is longer than how frequently it recurs.
                    End = DateTimeOffset.Parse("2024-9-1T00:00:01+08:00"), // This behavior is the same as the Outlook. Outlook uses 365 days as a yearly interval.
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.AbsoluteYearly,
                            Interval = 1,
                            DayOfMonth = 1,
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.End,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // The duration of the time window is longer than how frequently it recurs.
                    End = DateTimeOffset.Parse("2024-9-1T00:00:01+08:00"), // This behavior is the same as the Outlook. Outlook uses 365 days as a yearly interval.
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeYearly,
                            Interval = 1,
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Friday }, // 2023.9.1 is Friday.
                            Month = 9
                        },
                        Range = new RecurrenceRange()
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
                        Pattern = new RecurrencePattern(),
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.EndDate,
                            EndDate = DateTimeOffset.Parse("2023-8-31T00:00:00+08:00") // EndDate is earlier than the Start
                        }
                    }
                },
                ParamName.EndDate,
                ErrorMessage.OutOfRange ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T23:00:00+00:00"), // 2023-9-2 under the RecurrenceTimeZone
                    End = DateTimeOffset.Parse("2023-9-1T23:00:01+00:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern(),
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.EndDate,
                            EndDate = DateTimeOffset.Parse("2023-9-1"), // EndDate is earlier than the Start
                            RecurrenceTimeZone = "UTC+08:00" // All date time in the recurrence settings will be aligned to the RecurrenceTimeZone
                        }
                    }
                },
                ParamName.EndDate,
                ErrorMessage.OutOfRange )
            };

            ConsumeValidationTestData(testData);
        }

        [Fact]
        public void ValidTimeWindowAcrossWeeks()
        {
            var settings = new TimeWindowFilterSettings()
            {
                Start = DateTimeOffset.Parse("2024-1-16T00:00:00+08:00"), // Tuesday
                End = DateTimeOffset.Parse("2024-1-19T00:00:00+08:00"), // Time window duration is 3 days
                Recurrence = new Recurrence()
                {
                    Pattern = new RecurrencePattern()
                    {
                        Type = RecurrencePatternType.Weekly,
                        Interval = 1,
                        DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Tuesday, DayOfWeek.Saturday } // The time window duration should be shorter than 3 days because the gap between Saturday in the previous week and Tuesday in this week is 3 days.
                    },
                    Range = new RecurrenceRange()
                }
            };

            //
            // The settings is valid. No exception should be thrown.
            RecurrenceEvaluator.MatchRecurrence(DateTimeOffset.Now, settings);
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
                            Type = RecurrencePatternType.Weekly,
                            DaysOfWeek = Enumerable.Empty<DayOfWeek>()
                        },
                        Range = new RecurrenceRange()
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
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // 2023-9-1 is Friday. Start date is not a valid first occurrence.
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            DaysOfWeek = new List<DayOfWeek>{ DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday }
                        },
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.Start,
                ErrorMessage.NotMatched ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Start (2023-8-31T23:00:00+07:00) is Thursday under the RecurrenceTimeZone. Start date is not a valid first occurrence.
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            DaysOfWeek = new List<DayOfWeek>{ DayOfWeek.Friday }
                        },
                        Range = new RecurrenceRange()
                        {
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
                            Type = RecurrencePatternType.AbsoluteMonthly,
                        },
                        Range = new RecurrenceRange()
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
                    Start = DateTimeOffset.Parse("2023-9-2T00:00:00+08:00"), // Start date is not a valid first occurrence.
                    End = DateTimeOffset.Parse("2023-9-2T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.AbsoluteMonthly,
                            DayOfMonth = 1
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = Enumerable.Empty<DayOfWeek>()
                        },
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.DaysOfWeek,
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
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Start date is the 1st Friday in 2023 Sep, not a valid first occurrence.
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>{ DayOfWeek.Friday },
                            Index = WeekIndex.Second
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.AbsoluteYearly,
                            DayOfMonth = 1
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.AbsoluteYearly,
                            Month = 9
                        },
                        Range = new RecurrenceRange()
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
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Start date is not a valid first occurrence.
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.AbsoluteYearly,
                            DayOfMonth = 1,
                            Month = 8
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.RelativeYearly,
                            DaysOfWeek = Enumerable.Empty<DayOfWeek>(),
                            Month = 9
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.RelativeYearly,
                            DaysOfWeek = new List<DayOfWeek>{ DayOfWeek.Friday }
                        },
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.Month,
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
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Start date is the 1st Friday in 2023 Sep, not a valid first occurrence.
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeYearly,
                            DaysOfWeek = new List<DayOfWeek>{ DayOfWeek.Friday },
                            Month = 8
                        },
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.Start,
                ErrorMessage.NotMatched ),

                ( new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Start date is the 1st Friday in 2023 Sep, not a valid first occurrence.
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeYearly,
                            DaysOfWeek = new List<DayOfWeek>{ DayOfWeek.Friday },
                            Month = 9,
                            Index = WeekIndex.Second
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.Daily
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.Daily,
                            Interval = 2
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-5T00:00:00+08:00"), // Within the recurring time window 2023-9-5T00:00:00+08:00 ~ 2023-9-7T00:00:00+08:00.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Daily,
                            Interval = 4
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-6T00:00:00+08:00"), // Within the recurring time window 2023-9-5T00:00:00+08:00 ~ 2023-9-7T00:00:00+08:00.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Daily,
                            Interval = 4
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-9T00:00:00+08:00"),  // Within the recurring time window 2023-9-9T00:00:00+08:00 ~ 2023-9-11T00:00:00+08:00.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Daily,
                            Interval = 4
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Within the recurring time window 2023-9-3T00:00:00+08:00 ~ 2023-9-31T00:00:01+08:00.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Daily,
                            Interval = 2
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // The third occurrence.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Daily
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
                            NumberOfOccurrences = 2
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-2T17:00:00+00:00"), // 2023-9-3T01:00:00+08:00 under the RecurrenceTimeZone, which is beyond the EndDate
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T17:00:00+00:00"),
                    End = DateTimeOffset.Parse("2023-9-1T17:30:00+00:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Daily
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.EndDate,
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
                ( DateTimeOffset.Parse("2023-9-4T00:00:00+08:00"), // Monday in the 2nd week after the Start date
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Friday
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            // FirstDayOfWeek is Sunday by default.
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Friday }
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"), // Friday in the 2nd week after the Start date
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Friday
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            Interval = 2,
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Friday }
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-15T00:00:00+08:00"), // Friday in the 3rd week after the Start date
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Friday
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            Interval = 2,
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Friday }
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-4T00:00:00+08:00"), // Monday is not included in DaysOfWeek
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Friday
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-2T00:00:00+08:00"), // The 2nd occurrence.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
                            NumberOfOccurrences = 1
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"), // The 8th occurence.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Friday
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
                            NumberOfOccurrences = 7
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"), // The 8th occurence.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // Friday
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
                            NumberOfOccurrences = 8
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-4T00:00:00+08:00"), // Monday in the 2nd week after the Start date
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Sunday
                    End = DateTimeOffset.Parse("2023-9-3T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            Interval = 2,
                            FirstDayOfWeek = DayOfWeek.Monday, // 2023-9-3 is the last day of the 1st week after the Start date
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Sunday }
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-4T00:00:00+08:00"), // Monday in the 1st week after the Start date
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Sunday
                    End = DateTimeOffset.Parse("2023-9-3T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            Interval = 2,
                            // FirstDayOfWeek is Sunday by default, 2023-9-3 ~ 2023-9-9 is the 1st week after the Start date
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Sunday }
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-18T00:00:00+08:00"), // Monday in the 4th week after the Start date
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-3T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            Interval = 2,
                            FirstDayOfWeek = DayOfWeek.Monday, // 2023-9-3 1st week, 9-4 ~ 9-10 2nd week (Skipped), 9-11 ~ 9-17 3rd week, 9-18 ~ 9-24 4th week (Skipped)
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Sunday }
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-18T00:00:00+08:00"), // Monday in the 3rd week after the Start date
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Sunday
                    End = DateTimeOffset.Parse("2023-9-3T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            Interval = 2,
                            // FirstDayOfWeek is Sunday by default, 2023-9-3 ~ 9-9 1st week, 9-17 ~ 9-23 3rd week
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Sunday }
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-17T00:00:00+08:00"), // Sunday in the 3rd week after the Start date
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Sunday
                    End = DateTimeOffset.Parse("2023-9-3T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            Interval = 2,
                            FirstDayOfWeek = DayOfWeek.Monday, // 2023-9-3 1st week, 9-11 ~ 9-17 3rd week
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Sunday } // 2023-9-3, 9-11. 9-17
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
                            NumberOfOccurrences = 3
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-13T00:00:00+08:00"), // Within the recurring time window 2023-9-11T:00:00:00+08:00 ~ 2023-9-15T:00:00:00+08:00.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Sunday
                    End = DateTimeOffset.Parse("2023-9-7T00:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            Interval = 2,
                            FirstDayOfWeek = DayOfWeek.Monday,
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Sunday } // Time window occurrences: 9-3 ~ 9-7 (1st week), 9-11 ~ 9-15 and 9-17 ~ 9-21 (3rd week)
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-19T00:00:00+08:00"), // The 3rd occurrence: 2023-9-17T:00:00:00+08:00 ~ 2023-9-21T:00:00:00+08:00.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Sunday
                    End = DateTimeOffset.Parse("2023-9-7T00:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            Interval = 2,
                            FirstDayOfWeek = DayOfWeek.Monday,
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Sunday } // Time window occurrences: 9-3 ~ 9-7 (1st week), 9-11 ~ 9-15 and 9-17 ~ 9-21 (3rd week)
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
                            NumberOfOccurrences = 3
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-19T00:00:00+08:00"), // The 3rd occurrences
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Sunday
                    End = DateTimeOffset.Parse("2023-9-7T00:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            Interval = 2,
                            FirstDayOfWeek = DayOfWeek.Monday,
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Sunday } // Time window occurrences: 9-3 ~ 9-7 (1st week), 9-11 ~ 9-15 and 9-17 ~ 9-21 (3rd week)
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
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
                            Type = RecurrencePatternType.AbsoluteMonthly,
                            DayOfMonth = 1
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.AbsoluteMonthly,
                            DayOfMonth = 1,
                            Interval = 5 // 2023-9-1, 2024-2-1, 2024-7-1 ...
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2024-7-1T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.AbsoluteMonthly,
                            DayOfMonth = 1,
                            Interval = 5 // 2023-9-1, 2024-2-1, 2024-7-1 ...
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.AbsoluteMonthly,
                            DayOfMonth = 1,
                            Interval = 5 // 2023-9-1, 2024-2-1, 2024-7-1 ...
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2024-8-1T00:00:00+08:00"),
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.AbsoluteMonthly,
                            DayOfMonth = 1,
                            Interval = 5 // 2023-9-1, 2024-2-1, 2024-7-1 ...
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2024-9-1T00:00:00+08:00"), // The 4th occurrence.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.AbsoluteMonthly,
                            DayOfMonth = 1,
                            Interval = 4 // 2023-9-1, 2024-1-1, 2024-5-1, 2024-9-1 ...
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
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
                            Type = RecurrencePatternType.AbsoluteMonthly,
                            Interval = 2,
                            DayOfMonth = 29
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.EndDate,
                            EndDate = DateTimeOffset.Parse("2024-2-29")
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
                            Type = RecurrencePatternType.AbsoluteMonthly,
                            Interval = 2,
                            DayOfMonth = 29
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.EndDate,
                            EndDate = DateTimeOffset.Parse("2024-2-28")
                        }
                    }
                },
                false ),
            };

            ConsumeEvalutationTestData(testData);
        }

        [Fact]
        public void MatchRelativeMonthlyRecurrenceTest()
        {
            var testData = new List<ValueTuple<DateTimeOffset, TimeWindowFilterSettings, bool>>()
            {
                ( DateTimeOffset.Parse("2023-10-6T00:00:00+08:00"), // 1st Friday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // 1st Friday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday },
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"), // 2nd Friday in 2023 Sep 
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // 1st Friday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday }
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-10-13T00:00:00+08:00"), // 2nd Friday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"), // 1st Friday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-8T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday },
                            Index = WeekIndex.Second
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-10-13T00:00:00+08:00"), // 2nd Friday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"), // 1st Friday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-8T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday },
                            Index = WeekIndex.Second,
                            Interval = 3 // 2023-9, 2023-12 ...
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-12-8T00:00:00+08:00"), // 2nd Friday in 2023 Dec
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"), // 2nd Friday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-8T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday },
                            Index = WeekIndex.Second,
                            Interval = 3 // 2023-9, 2023-12 ...
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-15T00:00:00+08:00"), // 3rd Friday in 2023 Sep
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"), // 2nd Friday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-8T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday },
                            Index = WeekIndex.Second
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-10-27T00:00:00+08:00"), // 4th Friday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-29T00:00:00+08:00"), // 2nd Friday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-29T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday },
                            Index = WeekIndex.Last
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-11-24T00:00:00+08:00"), // 4th Friday in 2023 Nov
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-29T00:00:00+08:00"), // 5th Friday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-29T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday },
                            Index = WeekIndex.Last
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-12-29T00:00:00+08:00"), // 5th Friday in 2023 Dec
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-29T00:00:00+08:00"), // 5th Friday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-29T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday },
                            Index = WeekIndex.Last
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-10-29T00:00:00+08:00"), // 5th Sunday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-25T00:00:00+08:00"), // 4th Monday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-25T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Sunday, DayOfWeek.Monday },
                            Index = WeekIndex.Last
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-10-30T00:00:00+08:00"), // 5th Monday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-25T00:00:00+08:00"), // 4th Monday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-25T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Sunday, DayOfWeek.Monday },
                            Index = WeekIndex.Last
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-10-6T00:00:00+08:00"), // 1st Friday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // 1st Friday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday },
                            // Index is First by default.
                            Interval = 3
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-12-1T00:00:00+08:00"), // 1st Friday in 2023 Dec
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // 1st Friday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday },
                            // Index is First by default.
                            Interval = 3
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-10-2T00:00:00+08:00"), // 1st Monday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // 1st Friday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday, DayOfWeek.Monday },
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-10-6T00:00:00+08:00"), // 1st Friday in 2023 Oct
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // 1st Friday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday, DayOfWeek.Monday }
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ), // The time window will only occur on either 1st Monday or 1st Friday, the 1st Monday of 2023 Oct is 10.2 

                ( DateTimeOffset.Parse("2023-11-3T00:00:00+08:00"), // 1st Friday in 2023 Nov
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // 1st Friday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            Interval = 2,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday, DayOfWeek.Monday }
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-11-6T00:00:00+08:00"), // 1st Monday in 2023 Nov
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"), // 1st Friday in 2023 Sep
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            Interval = 2,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday, DayOfWeek.Monday }
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ), // The time window will only occur on either 1st Monday or 1st Friday, the 1st Monday of 2023 Nov is 11.3

                ( DateTimeOffset.Parse("2023-12-1T00:00:00+08:00"), // the first day of the month
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            Interval = 3,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.RelativeMonthly,
                            Interval = 3, // 2023-9, 2023-12, 2024-3 ...
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-12-1T00:00:00+08:00"), // The 4th occurrence.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
                            NumberOfOccurrences = 3
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-10-1T00:00:00+08:00"), // Sunday is not included in the DaysOfWeek
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday}
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-10-2T00:00:00+08:00"), // 1st Monday
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday}
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ), // 2023-10-1 is Sunday which is not included in the DaysOfWeek.

                ( DateTimeOffset.Parse("2023-10-2T00:00:00+08:00"), // 1st Monday,
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeMonthly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ) // The time window will occur on 2023-10-1
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
                            Type = RecurrencePatternType.AbsoluteYearly,
                            DayOfMonth = 1,
                            Month = 9
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.AbsoluteYearly,
                            DayOfMonth = 1,
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2024-9-1T00:00:00+08:00"), // The 2nd occurrence.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.AbsoluteYearly,
                            DayOfMonth = 1,
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
                            NumberOfOccurrences = 1
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2026-9-1T00:00:00+08:00"), // The 2nd occurrence.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.AbsoluteYearly,
                            Interval = 3, // 2023, 2026, ...
                            DayOfMonth = 1,
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
                            NumberOfOccurrences = 2
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2029-9-1T00:00:00+08:00"), // The 3rd occurrence.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.AbsoluteYearly,
                            Interval = 3, // 2023, 2026, 2029 ...
                            DayOfMonth = 1,
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
                            NumberOfOccurrences = 2
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
                ( DateTimeOffset.Parse("2024-9-6T00:00:00+08:00"), // 1st Friday in 2024 Sep
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeYearly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Friday },
                            Month = 9
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2024-9-1T00:00:00+08:00"), // 1st Sunday in 2024 Sep, Sunday is not included in the DaysOfWeek.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeYearly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
                            Month = 9
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.RelativeYearly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
                            Month = 9
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.RelativeYearly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
                            Month = 9
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.RelativeYearly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
                            Month = 9
                            // Index is First by default.
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.RelativeYearly,
                            Interval = 2, // 2023, 2025 ...
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
                            Month = 9
                        },
                        Range = new RecurrenceRange()
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
                            Type = RecurrencePatternType.RelativeYearly,
                            Interval = 3, // 2023, 2026 ...
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2026-9-1T00:00:00+08:00"), // The 4th occurrence.
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2023-9-1T00:00:00+08:00"),
                    End = DateTimeOffset.Parse("2023-9-1T00:00:01+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.RelativeYearly,
                            DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
                            Month = 9
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
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
