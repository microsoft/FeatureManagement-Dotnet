// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A context used by <see cref="IFeatureFilter"/> to gain insight into what feature flag is being evaluated and the parameters needed to check whether the feature flag should be enabled.
    /// </summary>
    public class FeatureFilterEvaluationContext
    {
        /// <summary>
        /// The name of the feature flag being evaluated.
        /// </summary>
        public string FeatureFlagName { get; set; }

        /// <summary>
        /// The settings provided for the feature filter to use when evaluating whether the feature flag should be enabled.
        /// </summary>
        public IConfiguration Parameters { get; set; }
    }
}
