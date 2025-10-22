// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.FeatureFilters;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Extensions for <see cref="IVariantFeatureManager"/>.
    /// </summary>
    public static class VariantFeatureManagerExtensions
    {
        /// <summary>
        /// Gets the assigned variant for a specific feature.
        /// </summary>
        /// <param name="variantFeatureManager">The <see cref="IVariantFeatureManager"/> instance.</param>
        /// <param name="feature">The name of the feature to evaluate.</param>
        /// <param name="context">An instance of <see cref="TargetingContext"/> used to evaluate which variant the user will be assigned.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A variant assigned to the user based on the feature's configured allocation.</returns>
        public static ValueTask<Variant> GetVariantAsync(this IVariantFeatureManager variantFeatureManager, string feature, TargetingContext context, CancellationToken cancellationToken = default)
        {
            return variantFeatureManager.GetVariantAsync(feature, context, cancellationToken);
        }
    }
}
