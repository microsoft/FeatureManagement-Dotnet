// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a method to assign a variant of a feature to be used based off of custom conditions.
    /// </summary>
    public interface IFeatureVariantAssigner : IFeatureVariantAssignerMetadata
    {
        /// <summary>
        /// Assign a variant of a feature to be used based off of customized criteria.
        /// </summary>
        /// <param name="variantAssignmentContext">A variant assignment context that contains information needed to assign a variant for a feature.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The variant that should be assigned for a given feature.</returns>
        ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, CancellationToken cancellationToken);
    }
}
