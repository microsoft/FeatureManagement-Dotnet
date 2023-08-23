// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Describes how a feature's state will be evaluated.
    /// </summary>
    public enum FeatureStatus
    {
        /// <summary>
        /// The state of the feature is conditional upon the feature evaluation pipeline.
        /// </summary>
        Conditional,
        /// <summary>
        /// The state of the feature is always disabled.
        /// </summary>
        Disabled
    }
}
