// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using System;
using System.Threading.Tasks;

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
        /// Enables notification of feature flags that are missing a configuration.
        /// If a feature flag exists with no configuration then this delegate will be invoked.
        /// </summary>
        public Func<string, Task> OnMissingFeatureConfiguration { get; set; }
    }
}
