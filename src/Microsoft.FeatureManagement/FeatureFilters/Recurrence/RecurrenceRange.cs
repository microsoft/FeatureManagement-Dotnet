// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// The recurrence range specifying how long the recurrence pattern repeats
    /// </summary>
    public class RecurrenceRange
    {
        /// <summary>
        /// The recurrence range type
        /// </summary>
        public string Type { get; set; } = "NoEnd";

        /// <summary>
        /// The date to stop applying the recurrence pattern
        /// </summary>
        public DateTimeOffset? EndDate { get; set; }

        /// <summary>
        /// The number of times to repeat the time window
        /// </summary>
        public int? NumberOfOccurrences { get; set; }

        /// <summary>
        /// Time zone for recurrence settings, e.g. UTC+08:00
        /// </summary>
        public string RecurrenceTimeZone { get; set; }
    }
}
