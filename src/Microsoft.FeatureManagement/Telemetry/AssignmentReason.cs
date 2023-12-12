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
        /// The reason the variant was assigned during the evaluation of a feature.
        /// </summary>
        None,

        /// <summary>
        /// The reason the variant was assigned during the evaluation of a feature.
        /// </summary>
        DisabledDefault,

        /// <summary>
        /// The reason the variant was assigned during the evaluation of a feature.
        /// </summary>
        EnabledDefault,

        /// <summary>
        /// The reason the variant was assigned during the evaluation of a feature.
        /// </summary>
        User,

        /// <summary>
        /// The reason the variant was assigned during the evaluation of a feature.
        /// </summary>
        Group,

        /// <summary>
        /// The reason the variant was assigned during the evaluation of a feature.
        /// </summary>
        Percentile
    }
}
