// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// The definition of how variants are allocated for a feature.
    /// </summary>
    public class Allocation
    {
        /// <summary>
        /// The default variant used if the feature is enabled and no variant is assigned.
        /// </summary>
        public string DefaultWhenEnabled { get; set; }

        /// <summary>
        /// The default variant used if the feature is disabled.
        /// </summary>
        public string DefaultWhenDisabled { get; set; }

        /// <summary>
        /// Describes a mapping of user ids to variants.
        /// </summary>
        public IEnumerable<UserAllocation> User { get; set; }

        /// <summary>
        /// Describes a mapping of group names to variants.
        /// </summary>
        public IEnumerable<GroupAllocation> Group { get; set; }

        /// <summary>
        /// Allocates percentiles of user base to variants.
        /// </summary>
        public IEnumerable<PercentileAllocation> Percentile { get; set; }

        /// <summary>
        /// Maps users to the same percentile across multiple feature flags.
        /// </summary>
        public string Seed { get; set; }
    }
}
