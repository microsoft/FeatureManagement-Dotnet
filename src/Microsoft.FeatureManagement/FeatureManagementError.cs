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
        /// A feature assigner that was listed for variant assignment was not found.
        /// </summary>
        MissingFeatureAssigner,

        /// <summary>
        /// An assigned feature variant does not have a valid configuration reference.
        /// </summary>
        MissingConfigurationReference,

        /// <summary>
        /// An invalid configuration was encountered when performing a feature managment operation.
        /// </summary>
        InvalidConfiguration
    }
}
