// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using System.Threading;

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

        /// <summary>
        /// A settings object, if any, provided for the feature filter to use when evaluating whether the feature should be enabled.
        /// This property is populated in two cases:
        /// <list type="bullet">
        /// <item>For features that provide parameters as an object, via <see cref="FeatureFilterConfiguration.ParametersObject"/>.</item>
        /// <item>For <see cref="IFeatureFilter"/>s that implement <see cref="IFilterParametersBinder"/>.</item>
        /// </list>
        /// </summary>
        public object Settings { get; set; }

        /// <summary>
        /// A cancellation token that can be used to request cancellation of the feature evaluation operation.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }
    }
}
