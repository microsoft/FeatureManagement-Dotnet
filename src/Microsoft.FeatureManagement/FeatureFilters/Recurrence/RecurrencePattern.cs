// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// The recurrence pattern specifying how often the time window repeats
    /// </summary>
    public class RecurrencePattern
    {
        /// <summary>
        /// The recurrence pattern type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The number of units between occurrences, where units can be in days, weeks, months, or years, depending on the pattern type
        /// </summary>
        public int Interval { get; set; } = 1;

        /// <summary>
        /// The days of the week on which the time window occurs
        /// </summary>
        public IEnumerable<string> DaysOfWeek { get; set; }

        /// <summary>
        /// The first day of the week.
        /// </summary>
        public string FirstDayOfWeek { get; set; } = "Sunday";

        /// <summary>
        /// Specifies on which instance of the allowed days specified in DaysOfWeek the time window occurs, counted from the first instance in the month
        /// </summary>
        public string Index { get; set; } = "First";

        /// <summary>
        /// The day of the month on which the time window occurs
        /// </summary>
        public int? DayOfMonth { get; set; }

        /// <summary>
        /// The month on which the time window occurs
        /// </summary>
        public int? Month { get; set; }
    }
}
