// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A provider of feature flag definitions.
    /// </summary>
    public interface IFeatureFlagDefinitionProvider
    {
        /// <summary>
        /// Retrieves the definition for a given feature flag.
        /// </summary>
        /// <param name="featureFlagName">The name of the feature flag to retrieve the definition for.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The feature flag's definition.</returns>	
        Task<FeatureFlagDefinition> GetFeatureFlagDefinitionAsync(string featureFlagName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves definitions for all feature flags.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An enumerator which provides asynchronous iteration over feature flag definitions.</returns>
        IAsyncEnumerable<FeatureFlagDefinition> GetAllFeatureFlagDefinitionsAsync(CancellationToken cancellationToken = default);
    }
}
