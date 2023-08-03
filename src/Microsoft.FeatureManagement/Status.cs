// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Describes how a feature's state will be evaluated.
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// The state of the feature is conditional on the rest of its definition.
        /// </summary>
        Conditional,
        /// <summary>
        /// The state of the feature is disabled.
        /// </summary>
        Disabled
    }
}
