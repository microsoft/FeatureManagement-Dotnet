// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A filter that can be used to determine whether some criteria is met to enable a feature. A feature filter is free to use any criteria available, such as process state or request content.
    /// Feature filters can be registered for a given feature and if any feature filter evaluates to true, that feature will be considered enabled.
    /// A contextual feature filter can take advantage of contextual data passed in from callers of the feature management system.
    /// A contextual feature filter will only be executed if a context that is assignable from TContext is available.
    /// </summary>
    public interface IContextualFeatureFilter<TContext> : IFeatureFilterMetadata
    {
        /// <summary>
        /// Evaluates the feature filter to see if the filter's criteria for being enabled has been satisfied.
        /// </summary>
        /// <param name="featureFilterContext">A feature filter evaluation context that contains information that may be needed to evaluate the filter. This context includes configuration, if any, for this filter for the feature being evaluated.</param>
        /// <param name="appContext">A context defined by the application that is passed in to the feature management system to provide contextual information for evaluating a feature's state.</param>
        /// <returns>True if the filter's criteria has been met, false otherwise.</returns>
        Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureFilterContext, TContext appContext);
    }
}
