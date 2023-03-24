// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// A basic audience definition describing a set of users and groups.
    /// </summary>
    public class BasicAudience
    {
        /// <summary>
        /// Includes users in the audience by name.
        /// </summary>
        public List<string> Users { get; set; }

        /// <summary>
        /// Includes users in the audience by group name.
        /// </summary>
        public List<string> Groups { get; set; }
    }
}
