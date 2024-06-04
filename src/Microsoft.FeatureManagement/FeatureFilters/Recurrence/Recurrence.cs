// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// A recurrence definition describing how time window recurs
    /// </summary>
    public class Recurrence
    {
        /// <summary>
        /// The recurrence pattern specifying how often the time window repeats
        /// </summary>
        public RecurrencePattern Pattern { get; set; }

        /// <summary>
        /// The recurrence range specifying how long the recurrence pattern repeats
        /// </summary>
        public RecurrenceRange Range { get; set; }
    }
}
