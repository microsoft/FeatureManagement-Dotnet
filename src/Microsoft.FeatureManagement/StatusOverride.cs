// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides the capability to override whether a feature is considered enabled or disabled when a variant is assigned.
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
