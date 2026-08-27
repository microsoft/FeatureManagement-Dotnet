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
        /// A configuration object that can be used as an alternative to <see cref="ConfigurationValue"/>.
        /// Custom <see cref="IFeatureDefinitionProvider"/> implementations can populate this property directly
        /// instead of constructing an <see cref="IConfiguration"/> instance.
        /// When set, variants should prefer this over <see cref="ConfigurationValue"/>.
        /// </summary>
        public object ConfigurationObject { get; set; }

        /// <summary>
        /// Overrides the state of the feature if this variant has been assigned.
        /// </summary>
        public StatusOverride StatusOverride { get; set; } = StatusOverride.None;
    }
}
