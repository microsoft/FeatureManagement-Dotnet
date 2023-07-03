// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

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
        /// The value of the configuration for this variant of the feature.
        /// </summary>
        public string ConfigurationValue { get; set; }

        /// <summary>
        /// A reference pointing to the configuration for this variant of the feature.
        /// </summary>
        public string ConfigurationReference { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public StatusOverride StatusOverride { get; set; }
    }
}