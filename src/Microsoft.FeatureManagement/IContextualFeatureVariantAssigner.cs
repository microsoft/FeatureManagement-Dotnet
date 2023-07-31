// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.FeatureFilters;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a method to assign a variant of a feature to be used based off of custom conditions.
    /// </summary>
    public interface IContextualFeatureVariantAssigner
    {
        /// <summary>
        /// Assign a variant of a feature to be used based off of customized criteria.
        /// </summary>
        /// <param name="featureDefinition">Contains all of the properties defined for a feature in feature management.</param>
        /// <param name="targetingContext">The targeting context used to determine which variant should be assigned.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The variant that should be assigned for a given feature.</returns>
        ValueTask<FeatureVariant> AssignVariantAsync(FeatureDefinition featureDefinition, TargetingContext targetingContext, CancellationToken cancellationToken = default);
    }
}
