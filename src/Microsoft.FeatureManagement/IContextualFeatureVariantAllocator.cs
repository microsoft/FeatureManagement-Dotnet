// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a method to allocate a variant of a feature to be used based off of custom conditions.
    /// </summary>
    /// <typeparam name="TContext">A custom type that the allocator requires to perform allocation</typeparam>
    public interface IContextualFeatureVariantAllocator<TContext> : IFeatureVariantAllocatorMetadata
    {
        /// <summary>
        /// Allocate a variant of a feature to be used based off of customized criteria.
        /// </summary>
        /// <param name="variantAllocationContext">A variant allocation context that contains information needed to allocate a variant for a feature.</param>
        /// <param name="appContext">A context defined by the application that is passed in to the feature management system to provide contextual information for allocating a variant of a feature.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The variant that should be allocated for a given feature.</returns>
        ValueTask<FeatureVariant> AllocateVariantAsync(FeatureVariantAllocationContext variantAllocationContext, TContext appContext, CancellationToken cancellationToken = default);
    }
}
