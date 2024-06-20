// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// The settings that are used to configure the <see cref="TimeWindowFilter"/> feature filter.
    /// </summary>
    public class TimeWindowFilterSettings
    {
        /// <summary>
        /// An optional start time used to determine when a feature configured to use the <see cref="TimeWindowFilter"/> feature filter should be enabled.
        /// If no start time is specified the time window is considered to have already started.
        /// </summary>
        public DateTimeOffset? Start { get; set; }

        /// <summary>
        /// An optional end time used to determine when a feature configured to use the <see cref="TimeWindowFilter"/> feature filter should be enabled.
        /// If no end time is specified the time window is considered to never end.
        /// </summary>
        public DateTimeOffset? End { get; set; }

        /// <summary>
        /// Add-on recurrence rule allows the time window defined by Start and End to recur.
        /// The rule specifies both how often the time window repeats and for how long.
        /// </summary>
        public Recurrence Recurrence { get; set; }
    }
}
