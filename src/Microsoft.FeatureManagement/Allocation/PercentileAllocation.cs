// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// The definition of a percentile allocation.
    /// </summary>
    public class PercentileAllocation
    {
        /// <summary>
        /// The name of the variant.
        /// </summary>
        public string Variant { get; set; }

        /// <summary>
        /// The inclusive lower bound of the percentage to which the variant will be assigned.
        /// </summary>
        public double From { get; set; }

        /// <summary>
        /// The exclusive upper bound of the percentage to which the variant will be assigned.
        /// </summary>
        public double To { get; set; }
    }
}
