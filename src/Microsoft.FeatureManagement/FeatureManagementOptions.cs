// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Options that control the behavior of the feature management system.
    /// </summary>
    public class FeatureManagementOptions
    {
        /// <summary>
        /// Controls the behavior of feature evaluation when dependent feature filters are missing.
        /// If missing feature filters are not ignored an exception will be thrown when attempting to evaluate a feature that depends on a missing feature filter.
        /// </summary>
        public bool IgnoreMissingFeatureFilters { get; set; }

        /// <summary>
        /// Controls the behavior of feature assignment when dependent feature assigners are missing.
        /// If missing feature assigners are not ignored an exception will be thrown when attempting to assign a variant of a feature that uses the missing assigner.
        /// </summary>
        public bool IgnoreMissingFeatureAssigners { get; set; }
    }
}
