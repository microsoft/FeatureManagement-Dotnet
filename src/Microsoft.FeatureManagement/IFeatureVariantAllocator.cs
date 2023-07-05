// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a method to allocate a variant of a dynamic feature to be used based off of custom conditions.
    /// </summary>
    public interface IFeatureVariantAllocator : IFeatureVariantAllocatorMetadata
    {
        /// <summary>
        /// Allocate a variant of a dynamic feature to be used based off of customized criteria.
        /// </summary>
        /// <param name="variantAllocationContext">A variant allocation context that contains information needed to allocate a variant for a dynamic feature.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The variant that should be allocated for a given dynamic feature.</returns>
        ValueTask<FeatureVariant> AllocateVariantAsync(FeatureVariantAllocationContext variantAllocationContext, CancellationToken cancellationToken = default);
    }
}