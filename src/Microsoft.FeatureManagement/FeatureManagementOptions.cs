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
        /// This option is invalid when used in combination with <see cref="RequirementType.All"/>
        /// The default value is false.
        /// </summary>
        public bool IgnoreMissingFeatureFilters { get; set; }

        /// <summary>
        /// Controls the behavior of feature evaluation when the target feature is missing.
        /// If missing features are not ignored an exception will be thrown when attempting to evaluate them.
        /// The default value is true.
        /// </summary>
        public bool IgnoreMissingFeatures { get; set; } = true;
    }
}
