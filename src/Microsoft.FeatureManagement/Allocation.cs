// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.FeatureFilters;
using System.Collections.Generic;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// 
    /// </summary>
    public class Allocation
    {
        /// <summary>
        /// Describes a mapping of user id to variant.
        /// </summary>
        public List<string> Users { get; set; }

        /// <summary>
        /// Describes a mapping of group names to variants.
        /// </summary>
        public List<GroupRollout> Groups { get; set; }

        /// <summary>
        /// Allocate a percentage of the user base to variants.
        /// </summary>
        public double Percentile { get; set; }
    }
}
