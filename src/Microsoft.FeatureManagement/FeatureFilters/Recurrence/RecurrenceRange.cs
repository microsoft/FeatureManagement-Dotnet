// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// The recurrence range describes a date range over which the time window repeats.
    /// </summary>
    public class RecurrenceRange
    {
        /// <summary>
        /// The recurrence range type.
        /// </summary>
        public RecurrenceRangeType Type { get; set; }

        /// <summary>
        /// The date to stop applying the recurrence pattern.
        /// </summary>
        public DateTimeOffset? EndDate { get; set; }

        /// <summary>
        /// The number of times to repeat the time window.
        /// </summary>
        public int? NumberOfOccurrences { get; set; }

        /// <summary>
        /// Time zone for recurrence settings. e.g. UTC+08:00
        /// </summary>
        public string RecurrenceTimeZone { get; set; }
    }
}
