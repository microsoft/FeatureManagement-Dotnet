// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to evaluate whether a feature flag is enabled or disabled.
    /// </summary>
    public interface IFeatureManager
    {
        /// <summary>
        /// Retrieves a list of feature flag names registered in the feature manager.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An enumerator which provides asynchronous iteration over the feature flag names registered in the feature manager.</returns>
        IAsyncEnumerable<string> GetFeatureFlagNamesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether a given feature flag is enabled.
        /// </summary>
        /// <param name="featureFlag">The name of the feature flag to check.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>True if the feature flag is enabled, otherwise false.</returns>
        Task<bool> IsEnabledAsync(string featureFlag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether a given feature flag is enabled.
        /// </summary>
        /// <param name="featureFlag">The name of the feature flag to check.</param>
        /// <param name="context">A context providing information that can be used to evaluate whether a feature flag should be on or off.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>True if the feature flag is enabled, otherwise false.</returns>
        Task<bool> IsEnabledAsync<TContext>(string featureFlag, TContext context, CancellationToken cancellationToken = default);
    }
}
