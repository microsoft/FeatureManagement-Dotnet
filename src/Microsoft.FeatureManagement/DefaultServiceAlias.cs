// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Aliases recognized by the variant service provider when matching an implementation against the feature flag status.
    /// </summary>
    public static class DefaultServiceAlias
    {
        /// <summary>
        /// The default alias used to resolve the variant service when the feature flag is enabled.
        /// </summary>
        public const string WhenEnabled = "Enabled";

        /// <summary>
        /// The default alias used to resolve the variant service when the feature flag is disabled.
        /// </summary>
        public const string WhenDisabled = "Disabled";
    }
}
