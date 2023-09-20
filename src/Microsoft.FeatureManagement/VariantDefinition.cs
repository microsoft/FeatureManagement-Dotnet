// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.Extensions.Configuration;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// The definition for a variant of a feature.
    /// </summary>
    public class VariantDefinition
    {
        /// <summary>
        /// The name of the variant.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The value of the configuration for this variant of the feature.
        /// </summary>
        public IConfigurationSection ConfigurationValue { get; set; }

        /// <summary>
        /// A reference pointing to the configuration for this variant of the feature.
        /// </summary>
        public string ConfigurationReference { get; set; }

        /// <summary>
        /// Overrides the state of the feature if this variant has been assigned.
        /// </summary>
        public StatusOverride StatusOverride { get; set; } = StatusOverride.None;
    }
}
