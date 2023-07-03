// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// 
    /// </summary>
    public class Allocation
    {
        /// <summary>
        /// 
        /// </summary>
        public string DefaultWhenEnabled { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string DefaultWhenDisabled { get; set; }

        /// <summary>
        /// Describes a mapping of user id to variant.
        /// </summary>
        public IEnumerable<User> User { get; set; }

        /// <summary>
        /// Describes a mapping of group names to variants.
        /// </summary>
        public IEnumerable<Group> Group { get; set; }

        /// <summary>
        /// Allocate a percentage of the user base to variants.
        /// </summary>
        public IEnumerable<Percentile> Percentile { get; set; }

        /// <summary>
        /// Maps users to the same percentile across multiple feature flags.
        /// </summary>
        public int Seed { get; set; }
    }
}
