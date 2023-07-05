// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using System.Collections.Generic;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// 
    /// </summary>
    public class User
    {
        /// <summary>
        /// The name of the variant.
        /// </summary>
        public string Variant { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<string> Users { get; set; }
    }
}
