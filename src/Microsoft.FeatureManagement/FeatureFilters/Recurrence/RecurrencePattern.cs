// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// The recurrence pattern describes the frequency by which the time window repeats.
    /// </summary>
    public class RecurrencePattern
    {
        /// <summary>
        /// The recurrence pattern type.
        /// </summary>
        public RecurrencePatternType Type { get; set; }

        /// <summary>
        /// The number of units between occurrences, where units can be in days or weeks, depending on the pattern type.
        /// </summary>
        public int Interval { get; set; } = 1;

        /// <summary>
        /// The days of the week on which the time window occurs.
        /// </summary>
        public IEnumerable<DayOfWeek> DaysOfWeek { get; set; }

        /// <summary>
        /// The first day of the week.
        /// </summary>
        public DayOfWeek FirstDayOfWeek { get; set; }
    }
}
