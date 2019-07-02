// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A context used by <see cref="IFeatureFilter"/> to gain insight into what feature is being evaluated and the parameters needed to check whether the feature should be enabled.
    /// </summary>
    public class FeatureFilterEvaluationContext
    {
        /// <summary>
        /// The name of the feature being evaluated.
        /// </summary>
        public string FeatureName { get; set; }

        /// <summary>
        /// The settings provided for the feature filter to use when evaluating whether the feature should be enabled.
        /// </summary>
        public IConfiguration Parameters { get; set; }
    }
}
