// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Describes how a variant service implementation is selected from the bound feature flag.
    /// </summary>
    public enum VariantServiceMatch
    {
        /// <summary>
        /// The implementation is selected based on the assigned variant name of the feature flag.
        /// </summary>
        Variant = 0,

        /// <summary>
        /// The implementation is selected based on the enabled status of the feature flag.
        /// </summary>
        Status = 1
    }
}
