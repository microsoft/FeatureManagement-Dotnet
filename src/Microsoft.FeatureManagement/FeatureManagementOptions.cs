// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Options that control the behavior of the feature management system.
    /// </summary>
    public class FeatureManagementOptions
    {
        /// <summary>
        /// Controls the behavior of feature evaluation when dependent feature filters are missing.
        /// If missing features filters are not ignored an exception will be thrown when attempting to evaluate a feature that depends on a missing feature filter.
        /// </summary>
        public bool IgnoreMissingFeatureFilters { get; set; }
    }
}
