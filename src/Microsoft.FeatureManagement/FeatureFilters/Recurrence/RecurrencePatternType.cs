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
        /// The pattern where the time window will repeat on the same day or days of the week, based on the number of weeks between each set of occurrences.
        /// </summary>
        Weekly
    }
}
