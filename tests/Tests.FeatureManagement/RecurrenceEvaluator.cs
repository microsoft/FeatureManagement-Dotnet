// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests.FeatureManagement
{
    class ErrorMessage
    {
        public const string ValueOutOfRange = "The value is out of the accepted range.";
        public const string UnrecognizableValue = "The value is unrecognizable.";
        public const string RequiredParameter = "Value cannot be null or empty.";
        public const string StartNotMatched = "Start date is not a valid first occurrence.";
        public const string TimeWindowDurationOutOfRange = "Time window duration cannot be longer than how frequently it occurs";
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
                ErrorMessage.ValueOutOfRange ),

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
                ErrorMessage.ValueOutOfRange ),

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
                            EndDate = DateTimeOffset.Parse("2023-8-31T23:59:59+08:00") // EndDate is earlier than the Start.
                        }
                    }
                },
                ParamName.EndDate,
                ErrorMessage.ValueOutOfRange )
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
                ErrorMessage.ValueOutOfRange ),

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
                ErrorMessage.TimeWindowDurationOutOfRange ),

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
                ErrorMessage.TimeWindowDurationOutOfRange ),

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
                            // FirstDayOfWeek is Sunday by default
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Sunday }
                        },
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.End,
                ErrorMessage.TimeWindowDurationOutOfRange ),

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
                            // FirstDayOfWeek is Sunday by default
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Monday, DayOfWeek.Saturday } // The time window duration should be shorter than 2 days because the gap between Saturday in the previous week and Monday in this week is 2 days.
                        },
                        Range = new RecurrenceRange()
                    }
                },
                ParamName.End,
                ErrorMessage.TimeWindowDurationOutOfRange ),

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
                ErrorMessage.TimeWindowDurationOutOfRange ),
            };

            ConsumeValidationTestData(testData);
        }

        [Fact]
        public void ValidTimeWindowAcrossWeeks()
        {
            var settings = new TimeWindowFilterSettings()
            {
                Start = DateTimeOffset.Parse("2024-1-16T00:00:00+08:00"), // Tuesday
                End = DateTimeOffset.Parse("2024-1-19T00:00:00+08:00"), // Time window duration is 3 days.
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

            settings = new TimeWindowFilterSettings()
            {
                Start = DateTimeOffset.Parse("2024-1-15T00:00:00+08:00"), // Monday
                End = DateTimeOffset.Parse("2024-1-19T00:00:00+08:00"), // Time window duration is 4 days.
                Recurrence = new Recurrence()
                {
                    Pattern = new RecurrencePattern()
                    {
                        Type = RecurrencePatternType.Weekly,
                        Interval = 2, // The interval is larger than one week, there is no across-week issue.
                        FirstDayOfWeek = DayOfWeek.Monday,
                        DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Sunday }
                    },
                    Range = new RecurrenceRange()
                }
            };

            //
            // The settings is valid. No exception should be thrown.
            RecurrenceEvaluator.MatchRecurrence(DateTimeOffset.Now, settings);

            settings = new TimeWindowFilterSettings()
            {
                Start = DateTimeOffset.Parse("2024-1-15T00:00:00+08:00"), // Monday
                End = DateTimeOffset.Parse("2024-1-19T00:00:00+08:00"), // Time window duration is 4 days.
                Recurrence = new Recurrence()
                {
                    Pattern = new RecurrencePattern()
                    {
                        Type = RecurrencePatternType.Weekly,
                        Interval = 1,
                        FirstDayOfWeek = DayOfWeek.Monday,
                        DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Sunday }
                    },
                    Range = new RecurrenceRange()
                }
            };

            //
            // The settings is invalid, since we change the interval to 1.
            Assert.Throws<ArgumentException>(
            () =>
            {
                RecurrenceEvaluator.MatchRecurrence(DateTimeOffset.Now, settings);
            });
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
        public void WeeklyPatternStartNotMatchedTest()
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
                ErrorMessage.StartNotMatched )
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

                ( DateTimeOffset.Parse("2023-9-5T00:00:00+08:00"), // Within the recurring time window 2023-9-5T00:00:00+08:00 ~ 2023-9-7T00:00:00+08:00
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

                ( DateTimeOffset.Parse("2023-9-6T00:00:00+08:00"), // Within the recurring time window 2023-9-5T00:00:00+08:00 ~ 2023-9-7T00:00:00+08:00
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

                ( DateTimeOffset.Parse("2023-9-9T00:00:00+08:00"),  // Within the recurring time window 2023-9-9T00:00:00+08:00 ~ 2023-9-11T00:00:00+08:00
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

                ( DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // Within the recurring time window 2023-9-3T00:00:00+08:00 ~ 2023-9-31T00:00:01+08:00
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

                ( DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // The third occurrence
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

                ( DateTimeOffset.Parse("2023-9-4T00:00:00+08:00"), // Monday is not included in DaysOfWeek.
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

                ( DateTimeOffset.Parse("2023-9-2T00:00:00+08:00"), // The 2nd occurrence
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

                ( DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // The 3rd occurrence
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
                            NumberOfOccurrences = 2
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2023-9-3T00:00:00+08:00"), // The 3rd occurrence
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
                            NumberOfOccurrences = 3
                        }
                    }
                },
                true ),

                ( DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"), // The 8th occurence
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

                ( DateTimeOffset.Parse("2023-9-8T00:00:00+08:00"), // The 8th occurence
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

                ( DateTimeOffset.Parse("2024-1-18T00:30:00+08:00"), // The 4th occurence
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2024-1-4T00:00:00+08:00"), // Thursday
                    End = DateTimeOffset.Parse("2024-1-4T01:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            // FirstDayOfWeek is Sunday by default.
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday},
                            Interval = 2
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
                            NumberOfOccurrences = 3
                        }
                    }
                },
                false ),

                ( DateTimeOffset.Parse("2024-1-18T00:30:00+08:00"), // The 4th occurence
                new TimeWindowFilterSettings()
                {
                    Start = DateTimeOffset.Parse("2024-1-4T00:00:00+08:00"), // Thursday
                    End = DateTimeOffset.Parse("2024-1-4T01:00:00+08:00"),
                    Recurrence = new Recurrence()
                    {
                        Pattern = new RecurrencePattern()
                        {
                            Type = RecurrencePatternType.Weekly,
                            // FirstDayOfWeek is Sunday by default.
                            DaysOfWeek = new List<DayOfWeek>(){ DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday},
                            Interval = 2
                        },
                        Range = new RecurrenceRange()
                        {
                            Type = RecurrenceRangeType.Numbered,
                            NumberOfOccurrences = 4
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

                ( DateTimeOffset.Parse("2023-9-13T00:00:00+08:00"), // Within the recurring time window 2023-9-11T:00:00:00+08:00 ~ 2023-9-15T:00:00:00+08:00
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

                ( DateTimeOffset.Parse("2023-9-19T00:00:00+08:00"), // The 3rd occurrence: 2023-9-17T:00:00:00+08:00 ~ 2023-9-21T:00:00:00+08:00
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

                ( DateTimeOffset.Parse("2023-9-19T00:00:00+08:00"), // The 3rd occurrence
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
    }
}
