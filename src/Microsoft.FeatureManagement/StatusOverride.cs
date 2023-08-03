// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Overrides the feature's state with this value when a variant has been assigned.
    /// </summary>
    public enum StatusOverride
    {
        /// <summary>
        /// Does not affect the feature state.
        /// </summary>
        None,
        /// <summary>
        /// The feature will be considered enabled.
        /// </summary>
        Enabled,
        /// <summary>
        /// The feature will be considered disabled.
        /// </summary>
        Disabled
    }
}
