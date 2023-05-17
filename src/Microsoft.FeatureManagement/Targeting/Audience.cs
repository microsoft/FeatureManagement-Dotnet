// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// An audience definition describing a set of users.
    /// </summary>
    public class Audience
    {
        /// <summary>
        /// Includes users in the audience by name.
        /// </summary>
        public List<string> Users { get; set; }

        /// <summary>
        /// Includes users in the audience based off a group rollout.
        /// </summary>
        public List<GroupRollout> Groups { get; set; }

        /// <summary>
        /// Includes users in the audience based off a percentage of the total possible audience. Valid values range from 0 to 100 inclusive.
        /// </summary>
        public double DefaultRolloutPercentage { get; set; }

        /// <summary>
        /// Excludes a basic audience from this audience.
        /// </summary>
        public BasicAudience Exclusion { get; set; }
    }
}
