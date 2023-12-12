// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement.Telemetry
{
    /// <summary>
    /// The reason the variant was assigned during the evaluation of a feature.
    /// </summary>
    public enum AssignmentReason
    {
        /// <summary>
        /// No variant is assigned.
        /// </summary>
        None,

        /// <summary>
        /// Variant is assigned by default after processing the user/group/percentile allocation, when the feature flag is disabled.
        /// </summary>
        DisabledDefault,

        /// <summary>
        /// Variant is assigned by default after processing the user/group/percentile allocation, when the feature flag is enabled.
        /// </summary>
        EnabledDefault,

        /// <summary>
        /// Variant is assigned because of the user allocation.
        /// </summary>
        User,

        /// <summary>
        /// Variant is assigned because of the group allocation.
        /// </summary>
        Group,

        /// <summary>
        /// Variant is assigned because of the percentile allocation.
        /// </summary>
        Percentile
    }
}
