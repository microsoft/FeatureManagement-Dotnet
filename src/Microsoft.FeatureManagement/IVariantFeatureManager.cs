// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading.Tasks;
using System.Threading;
using Microsoft.FeatureManagement.FeatureFilters;

namespace Microsoft.FeatureManagement
{
    public interface IVariantFeatureManager
    {
        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        Task<bool> IsEnabledAsync(string feature);

        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="context">A context providing information that can be used to evaluate whether a feature should be on or off.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        Task<bool> IsEnabledAsync<TContext>(string feature, TContext context);

        /// <summary>
        /// Gets the assigned variant for a specfic feature.
        /// </summary>
        /// <param name="feature">The name of the feature from which the variant will be assigned.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A variant assigned to the user based on the feature's allocation logic.</returns>
        ValueTask<Variant> GetVariantAsync(string feature, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the assigned variant for a specfic feature.
        /// </summary>
        /// <param name="feature">The name of the feature from which the variant will be assigned.</param>
        /// <param name="context">A context providing information that can be used to evaluate which variant the user will be assigned.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A variant assigned to the user based on the feature's allocation logic.</returns>
        ValueTask<Variant> GetVariantAsync(string feature, TargetingContext context, CancellationToken cancellationToken = default);
    }
}
