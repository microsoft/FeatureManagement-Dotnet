// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Specifies the aliases used by a variant service provider to resolve an implementation based on the feature flag status when no allocated variant matches.
    /// </summary>
    public class VariantServiceProviderOptions
    {
        /// <summary>
        /// The alias used to resolve the variant service when the feature flag is enabled and no allocated variant matches.
        /// </summary>
        public object FallbackWhenEnabled { get; set; } = true;

        /// <summary>
        /// The alias used to resolve the variant service when the feature flag is disabled and no allocated variant matches.
        /// </summary>
        public object FallbackWhenDisabled { get; set; } = false;
    }
}
