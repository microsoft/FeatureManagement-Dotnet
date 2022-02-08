// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A variant of a feature.
    /// </summary>
    public class FeatureVariant
    {
        /// <summary>
        /// The name of the variant.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Determines whether this variant should be chosen by default if no variant is chosen during the assignment process.
        /// </summary>
        public bool Default { get; set; }

        /// <summary>
        /// The parameters to be used during assignment to test whether the variant should be used.
        /// </summary>
        public IConfiguration AssignmentParameters { get; set; }

        /// <summary>
        /// A reference pointing to the configuration for this variant of the feature.
        /// </summary>
        public string ConfigurationReference { get; set; }
    }
}
