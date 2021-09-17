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
        /// An invalid configuration was encountered when performing a feature management operation.
        /// </summary>
        InvalidConfiguration,

        /// <summary>
        /// A feature variant assigner being used in feature evaluation is invalid.
        /// </summary>
        InvalidFeatureVariantAssigner,

        /// <summary>
        /// A feature that was requested for evaluation was not found.
        /// </summary>
        MissingFeature
    }
}
