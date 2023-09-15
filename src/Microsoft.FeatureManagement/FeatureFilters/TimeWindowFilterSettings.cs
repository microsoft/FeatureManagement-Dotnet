// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;

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
        /// An optional list of Cron expressions that can be used to filter out what time within the time window is applicable.
        /// </summary>
        public IEnumerable<string> Filters { get; set; } 
    }
}
