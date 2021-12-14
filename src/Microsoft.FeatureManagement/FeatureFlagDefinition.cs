// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// The definition of a feature flag.
    /// </summary>
    public class FeatureFlagDefinition
    {
        /// <summary>
        /// The name of the feature flag.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The feature filters that the feature flag can be enabled for.
        /// </summary>
        public IEnumerable<FeatureFilterConfiguration> EnabledFor { get; set; } = Enumerable.Empty<FeatureFilterConfiguration>();
    }
}
