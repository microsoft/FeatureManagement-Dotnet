// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// The definition of a filter allocation.
    /// </summary>
    public class ContextualFilterAllocation : FeatureFilterConfiguration
    {
        /// <summary>
        /// The name of the variant.
        /// </summary>
        public string Variant { get; set; }
    }
}
