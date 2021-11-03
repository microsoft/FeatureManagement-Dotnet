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
        /// A feature variant assigner that was listed for variant assignment was not found.
        /// </summary>
        MissingFeatureVariantAssigner,

        /// <summary>
        /// The feature variant assigner configured for the feature being evaluated is an ambiguous reference to multiple registered feature variant assigners.
        /// </summary>
        AmbiguousFeatureVariantAssigner,

        /// <summary>
        /// An assigned feature variant does not have a valid configuration reference.
        /// </summary>
        MissingConfigurationReference,

        /// <summary>
        /// A feature variant assigner being used in feature evaluation is invalid.
        /// </summary>
        InvalidFeatureVariantAssigner,

        /// <summary>
        /// A feature that was requested for evaluation was not found.
        /// </summary>
        MissingFeature,

        /// <summary>
        /// A dynamic feature does not have any feature variants registered.
        /// </summary>
        MissingFeatureVariant,

        /// <summary>
        /// A dynamic feature has multiple default feature variants configured.
        /// </summary>
        AmbiguousDefaultFeatureVariant,

        /// <summary>
        /// A dynamic feature does not have a default feature variant configured.
        /// </summary>
        MissingDefaultFeatureVariant
    }
}
