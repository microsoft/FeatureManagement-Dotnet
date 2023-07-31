// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.FeatureFilters;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a method to allocate a variant of a feature to be used based off of custom conditions.
    /// </summary>
    public interface IContextualFeatureVariantAllocator
    {
        /// <summary>
        /// Allocate a variant of a feature to be used based off of customized criteria.
        /// </summary>
        /// <param name="featureDefinition">Contains all of the properties defined for a feature in feature management.</param>
        /// <param name="targetingContext">The targeting context used to determine which variant should be allocated.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The variant that should be allocated for a given feature.</returns>
        ValueTask<FeatureVariant> AllocateVariantAsync(FeatureDefinition featureDefinition, TargetingContext targetingContext, CancellationToken cancellationToken = default);
    }
}
