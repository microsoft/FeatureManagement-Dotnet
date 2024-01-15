// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// The type of <see cref="RecurrencePattern"/> specifying the frequency by which the time window repeats.
    /// </summary>
    public enum RecurrencePatternType
    {
        /// <summary>
        /// The pattern where the time window will repeat based on the number of days specified by interval between occurrences.
        /// </summary>
        Daily,

        /// <summary>
        /// The pattern where the time window will repeat on the same day or days of the week, based on the number of weeks between each set of occurrences
        /// </summary>
        Weekly,

        /// <summary>
        /// The pattern where the time window will repeat on the specified day of the month, based on the number of months between occurrences.
        /// </summary>
        AbsoluteMonthly,

        /// <summary>
        /// The pattern where the time window will repeat on the specified days of the week, in the same relative position in the month, based on the number of months between occurrences.
        /// </summary>
        RelativeMonthly,

        /// <summary>
        /// The pattern where the time window will repeat on the specified day and month, based on the number of years between occurrences.
        /// </summary>
        AbsoluteYearly,

        /// <summary>
        /// The pattern where the time window will repeat on the specified days of the week, in the same relative position in a specific month of the year, based on the number of years between occurrences.
        /// </summary>
        RelativeYearly
    }
}
