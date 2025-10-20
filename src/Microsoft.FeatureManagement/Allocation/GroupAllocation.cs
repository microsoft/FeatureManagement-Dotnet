// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using System.Collections.Generic;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// The definition of a group allocation.
    /// </summary>
    public class GroupAllocation
    {
        /// <summary>
        /// The name of the variant.
        /// </summary>
        public string Variant { get; set; }

        /// <summary>
        /// A list of groups that can be assigned this variant.
        /// </summary>
        public IEnumerable<string> Groups { get; set; }
    }
}
