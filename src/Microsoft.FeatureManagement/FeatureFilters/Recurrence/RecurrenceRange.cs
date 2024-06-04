// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

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
        public DateTimeOffset EndDate { get; set; } = DateTimeOffset.MaxValue;

        /// <summary>
        /// The number of times to repeat the time window.
        /// </summary>
        public int NumberOfOccurrences { get; set; } = int.MaxValue;
    }
}
