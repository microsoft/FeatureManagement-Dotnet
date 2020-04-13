// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// Defines a percentage of a group to be included in a rollout.
    /// </summary>
    public class GroupRollout
    {
        /// <summary>
        /// The name of the group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The percentage of the group that should be considered part of the rollout.
        /// </summary>
        public double RolloutPercentage { get; set; }
    }
}
