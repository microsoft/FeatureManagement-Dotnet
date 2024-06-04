// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// The type of <see cref="RecurrenceRange"/> specifying the date range over which the time window repeats.
    /// </summary>
    public enum RecurrenceRangeType
    {
        /// <summary>
        /// The time window repeats on all the days that fit the corresponding <see cref="RecurrencePattern"/>.
        /// </summary>
        NoEnd,

        /// <summary>
        /// The time window repeats on all the days that fit the corresponding <see cref="RecurrencePattern"/> before or on the end date specified in EndDate of <see cref="RecurrenceRange"/>.
        /// </summary>
        EndDate,

        /// <summary>
        /// The time window repeats for the number specified in the NumberOfOccurrences of <see cref="RecurrenceRange"/> that fit based on the <see cref="RecurrencePattern"/>.
        /// </summary>
        Numbered
    }
}
