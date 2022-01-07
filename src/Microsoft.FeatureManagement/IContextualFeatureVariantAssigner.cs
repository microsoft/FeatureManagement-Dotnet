// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a method to assign a variant of a dynamic feature to be used based off of custom conditions.
    /// </summary>
    /// <typeparam name="TContext">A custom type that the assigner requires to perform assignment</typeparam>
    public interface IContextualFeatureVariantAssigner<TContext> : IFeatureVariantAssignerMetadata
    {
        /// <summary>
        /// Assign a variant of a dynamic feature to be used based off of customized criteria.
        /// </summary>
        /// <param name="variantAssignmentContext">A variant assignment context that contains information needed to assign a variant for a dynamic feature.</param>
        /// <param name="appContext">A context defined by the application that is passed in to the feature management system to provide contextual information for assigning a variant of a dynamic feature.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The variant that should be assigned for a given dynamic feature.</returns>
        ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, TContext appContext, CancellationToken cancellationToken = default);
    }
}
