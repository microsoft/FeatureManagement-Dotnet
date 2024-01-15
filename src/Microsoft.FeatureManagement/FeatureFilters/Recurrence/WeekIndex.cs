// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// The relative position in the month of the allowed days specified in DaysOfWeek of <see cref="RecurrencePattern"/> the recurrence occurs, counted from the first instance in the month.
    /// </summary>
    public enum WeekIndex
    {
        /// <summary>
        /// Specifies on the first instance of the allowed day of week, counted from the first instance in the month.
        /// </summary>
        First,

        /// <summary>
        /// Specifies on the second instance of the allowed day of week, counted from the first instance in the month.
        /// </summary>
        Second,

        /// <summary>
        /// Specifies on the third instance of the allowed day of week, counted from the first instance in the month.
        /// </summary>
        Third,

        /// <summary>
        /// Specifies on the fourth instance of the allowed day of week, counted from the first instance in the month.
        /// </summary>
        Fourth,

        /// <summary>
        /// Specifies on the last instance of the allowed day of week, counted from the first instance in the month.
        /// </summary>
        Last
    }
}
