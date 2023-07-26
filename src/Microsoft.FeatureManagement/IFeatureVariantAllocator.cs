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
    public interface IFeatureVariantAllocator : IFeatureVariantAllocatorMetadata
    {
        /// <summary>
        /// Allocate a variant of a feature to be used based off of customized criteria.
        /// </summary>
        /// <param name="variantAllocationContext">A variant allocation context that contains information needed to allocate a variant for a feature.</param>
        /// <param name="isFeatureEnabled">A boolean indicating whether the feature the variant is being allocated to is enabled.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The variant that should be allocated for a given feature.</returns>
        ValueTask<FeatureVariant> AllocateVariantAsync(FeatureVariantAllocationContext variantAllocationContext, bool isFeatureEnabled, CancellationToken cancellationToken = default);
    }
}
