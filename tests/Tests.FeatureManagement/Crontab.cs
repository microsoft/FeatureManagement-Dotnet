// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.FeatureManagement.FeatureFilters.Cron;
using System;
using Xunit;

namespace Tests.FeatureManagement
{
    public class CrontabTest
    {
        [Fact]
        public void CronFieldTest()
        {
            Assert.True(CronField.TryParse(CronFieldKind.Minute, "0", out CronField minuteField));
            Assert.True(minuteField.Match(0));
            for (int i = 1; i < 60; i++)
            {
                Assert.False(minuteField.Match(i));
            }

            Assert.True(CronField.TryParse(CronFieldKind.Hour, "*/2", out CronField hourField));
            Assert.False(hourField.MatchAll);
            for (int i = 0; i < 24; i++)
            {
                if (i % 2 == 0)
                {
                    Assert.True(hourField.Match(i));
                }
                else
                {
                    Assert.False(hourField.Match(i));
                }
            }

            Assert.True(CronField.TryParse(CronFieldKind.DayOfMonth, "1-3", out CronField dayOfMonthField));
            Assert.True(dayOfMonthField.Match(1));
            Assert.True(dayOfMonthField.Match(2));
            Assert.True(dayOfMonthField.Match(3));
            for (int i = 4; i < 32; i++)
            {
                Assert.False(dayOfMonthField.Match(i));
            }

            Assert.True(CronField.TryParse(CronFieldKind.Month, "1,2", out CronField monthField));
            Assert.True(monthField.Match(1));
            Assert.True(monthField.Match(2));
            for (int i = 3; i < 13; i++)
            {
                Assert.False(monthField.Match(i));
            }

            Assert.True(CronField.TryParse(CronFieldKind.DayOfWeek, "7", out CronField dayOfWeekField));
            Assert.True(dayOfWeekField.Match(0));
            for (int i = 1; i < 7; i++)
            {
                Assert.False(dayOfWeekField.Match(i));
            }
        }

        [Theory]
        [InlineData("* * * * *", true)]
        [InlineData("1 2 3 Apr Fri", true)]
        [InlineData("00-59/3,1,1,1,2-2 01,3,20-23,*,* */10,1-31/100 Apr,1-Feb,oct-DEC/1 Sun-Sat/2,Mon-Sun,0-7", true)]
        [InlineData("* * 2-1 * *", false)]
        [InlineData("Fri * * * *", false)]
        [InlineData("1 2 Wed 4 5", false)]
        [InlineData("* * * * * *", false)]
        [InlineData("* * * 1,2", false)]
        [InlineData("* * * 1, *", false)]
        [InlineData("* * * ,2 *", false)]
        [InlineData("* * , * *", false)]
        [InlineData("* * */-1 * *", false)]
        [InlineData("*****", false)]
        [InlineData("* * * # *", false)]
        [InlineData("0-60 * * * *", false)]
        [InlineData("* 24 * * *", false)]
        [InlineData("* * 32 * *", false)]
        [InlineData("* * * 13 *", false)]
        [InlineData("* * * * 8", false)]
        [InlineData("* * 0 * *", false)]
        [InlineData("* * * 0 *", false)]
        [InlineData("* * * */ *", false)]
        [InlineData("* * * *// *", false)]
        [InlineData("* * * --- *", false)]
        [InlineData("* * * - *", false)]
        public void ValidateCronExpressionTest(string expression, bool expected)
        {
            bool result = ValidateExpression(expression);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("* * * * *", "Mon, 28 Aug 2023 21:00:00 +08:00", true)]
        [InlineData("* * * * 0", "Sun, 3 Sep 2023 21:00:00 +08:00", true)]
        [InlineData("* * * * 7", "Sun, 3 Sep 2023 21:00:00 +08:00", true)]
        [InlineData("* * * 9 *", "2023-09-13T14:30:00+08:00", true)]
        [InlineData("* * 13 * *", "2023-09-13T14:30:00+08:00", true)]
        [InlineData("* * 13 9 *", "2023-09-13T14:30:00+08:00", true)]
        [InlineData("* * 13 8 *", "2023-09-13T14:30:00+08:00", false)]
        [InlineData("0 21 13 9 */2", "Tue, 12 Sep 2023 21:00:30 +08:00", true)]
        [InlineData("* * 13 9 *,1", "Mon, 11 Sep 2023 21:00:30 +08:00", false)]
        [InlineData("* * 13 9 0-6", "Mon, 11 Sep 2023 21:00:30 +08:00", true)]
        [InlineData("* * 13 9 Mon", "Wed, 13 Sep 2023 21:00:30 +08:00", true)]
        [InlineData("* * * * */2", "Sun, 3 Sep 2023 21:00:00 +08:00", true)]
        [InlineData("* * * * */2", "Mon, 4 Sep 2023 21:00:00 +08:00", false)]
        [InlineData("* * 4 * */2", "Mon, 4 Sep 2023 21:00:00 +08:00", true)]
        [InlineData("0 21 31 Aug Mon", "Thu, 31 Aug 2023 21:00:30 +08:00", true)]
        [InlineData("* 16-19 31 Aug Thu", "Thu, 31 Aug 2023 21:00:00 +08:00", false)]
        [InlineData("* * * 8 2", "Tue, 12 Sep 2023 21:00:30 +08:00", false)]
        [InlineData("00 21 30 * 0-6/2", "Tue, 12 Sep 2023 21:00:30 +08:00", true)]
        [InlineData("0-29 20-23 1,2,15-30/2 Jun-Sep Mon", "Wed, 30 Aug 2023 21:00:00 +08:00", false)]
        [InlineData("0-29 20-23 1,2,15-30/2 8,1-2,Sep,12 Mon", "2023-09-29T20:00:00+08:00", true)]
        [InlineData("0-29 21 * * *", "Thu, 31 Aug 2023 21:30:00 +08:00", false)]
        [InlineData("* * 2 9 0-Sat", "Fri, 1 Sep 2023 21:00:00 +08:00", true)]
        public void IsCronExpressionSatisfiedByTimeTest(string expression, string timeString, bool expected)
        {
            Assert.True(ValidateExpression(expression));

            bool result = IsCronExpressionSatisfiedByTime(expression, timeString);
            Assert.Equal(expected, result);
        }

        private bool ValidateExpression(string expression)
        {
            try
            {
                CronExpression cronExpression = CronExpression.Parse(expression);
                return true;
            }
            catch (Exception _)
            {
                return false;
            }
        }

        private bool IsCronExpressionSatisfiedByTime(string expression, string timeString)
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.Parse(timeString);
            CronExpression cronExpression = CronExpression.Parse(expression);
            return cronExpression.IsSatisfiedBy(dateTimeOffset);
        }
    }
}
