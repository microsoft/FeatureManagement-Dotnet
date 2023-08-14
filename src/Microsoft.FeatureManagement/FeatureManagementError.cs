// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// An error that can occur during feature management.
    /// </summary>
    public enum FeatureManagementError
    {
        /// <summary>
        /// A feature filter that was listed for feature evaluation was not found.
        /// </summary>
        MissingFeatureFilter,

        /// <summary>
        /// A feature filter configured for the feature being evaluated is an ambiguous reference to multiple registered feature filters.
        /// </summary>
        AmbiguousFeatureFilter,

        /// <summary>
        /// A feature that was requested for evaluation was not found.
        /// </summary>
        MissingFeature,

        /// <summary>
        /// There was a conflict in the feature management system.
        /// </summary>
        Conflict,

        /// <summary>
        /// The given configuration setting was invalid.
        /// </summary>
        InvalidConfigurationSetting,

        /// <summary>
        /// A feature does not have any feature variants registered when attempting to resolve the variant.
        /// </summary>
        MissingVariantDefinition,

        /// <summary>
        /// A feature variant specifies both a configuration reference and configuration value, or there is no valid configuration to reference.
        /// </summary>
        InvalidVariantConfiguration,

        /// <summary>
        /// A feature does not have allocation defined when attempting to resolve the variant.
        /// </summary>
        MissingAllocation
    }
}
