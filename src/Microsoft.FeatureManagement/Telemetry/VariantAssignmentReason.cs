// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement.Telemetry
{
    /// <summary>
    /// The reason the variant was assigned during the evaluation of a feature.
    /// </summary>
    public enum VariantAssignmentReason
    {
        /// <summary>
        /// Variant allocation did not happen. No variant is assigned.
        /// </summary>
        None,

        /// <summary>
        /// The default variant is assigned when a feature flag is disabled.
        /// </summary>
        DefaultWhenDisabled,

        /// <summary>
        /// The default variant is assigned because of no applicable user/group/percentile allocation when a feature flag is enabled.
        /// </summary>
        DefaultWhenEnabled,

        /// <summary>
        /// The variant is assigned because of the user allocation when a feature flag is enabled.
        /// </summary>
        User,

        /// <summary>
        /// The variant is assigned because of the group allocation when a feature flag is enabled.
        /// </summary>
        Group,

        /// <summary>
        /// The variant is assigned because of the percentile allocation when a feature flag is enabled.
        /// </summary>
        Percentile,

        /// <summary>
        /// The variant is assigned because of a matching filter.
        /// </summary>
        Filter
    }
}
