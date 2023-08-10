// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

namespace Microsoft.FeatureManagement.VariantAllocation
{
    /// <summary>
    /// The definition of a percentile allocation.
    /// </summary>
    public class Percentile
    {
        /// <summary>
        /// The name of the variant.
        /// </summary>
        public string Variant { get; set; }

        /// <summary>
        /// The lower bound of the percentage to which the variant will be assigned.
        /// </summary>
        public double From { get; set; }

        /// <summary>
        /// The upper bound of the percentage to which the variant will be assigned.
        /// </summary>
        public double To { get; set; }
    }
}
