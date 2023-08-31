// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

namespace Microsoft.FeatureManagement.FeatureFilters.Crontab
{
    /// <summary>
    /// Define an enum of all required fields of the Crontab expression.
    /// </summary>
    public enum CrontabFieldKind
    {
        /// <summary>
        /// Field Name: Minute
        /// Allowed Values: 0-59
        /// </summary>
        Minute,

        /// <summary>
        /// Field Name: Hour
        /// Allowed Values: 1-12
        /// </summary>
        Hour,

        /// <summary>
        /// Field Name: Day of month
        /// Allowed Values: 1-31
        /// </summary>
        DayOfMonth,

        /// <summary>
        /// Field Name: Month
        /// Allowed Values: 1-12 (or use the first three letters of the month name)
        /// </summary>
        Month,

        /// <summary>
        /// Field Name: Day of week
        /// Allowed Values: 0-7 (0 or 7 is Sunday, or use the first three letters of the day name)
        /// </summary>
        DayOfWeek
    }

}