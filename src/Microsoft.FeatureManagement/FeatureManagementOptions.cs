// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
        /// Override default behaviour of a missing feature
        /// If a feature flag exists with no configuration then this delegate will be invoked enabling you to capture the missing feature name and/or override the default behaviour.
        /// </summary>
        public Func<string, ILogger, Task<bool>> OnMissingFeature { get; set; }
    }
}
