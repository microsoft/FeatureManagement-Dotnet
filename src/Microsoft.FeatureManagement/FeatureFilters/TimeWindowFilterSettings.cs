// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.FeatureManagement.FeatureFilters.Crontab;
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
        public DateTimeOffset? Start { get; set; } // E.g. "Wed, 01 May 2019 22:59:30 GMT+0800"

        /// <summary>
        /// An optional end time used to determine when a feature configured to use the <see cref="TimeWindowFilter"/> feature filter should be enabled.
        /// If no end time is specified the time window is considered to never end.
        /// </summary>
        public DateTimeOffset? End { get; set; } // E.g. "Wed, 01 May 2019 23:00:00 GMT"

        /// <summary>
        /// An optional list which specifies the recurring time windows used to determine when a feature configured to use the <see cref="TimeWindowFilter"/> feature filter should be enabled.
        /// The recurring time windows are represented in the form of Crontab expression.
        /// If any recurring time window filter is specified, to enable the feature flag, the current time also needs to be within at least one of the recurring time windows.
        /// </summary>
        public List<CrontabExpression> Filters { get; set; } = new List<CrontabExpression>(); // E.g. ["* 18-19 * * Mon"] which means the recurring time window of 18:00~20:00 on Monday 
    }
}
