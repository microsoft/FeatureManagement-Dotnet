// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A provider of dynamic feature definitions.
    /// </summary>
    public interface IDynamicFeatureDefinitionProvider
    {
        /// <summary>
        /// Retrieves the definition for a given dynamic feature.
        /// </summary>
        /// <param name="dynamicFeatureName">The name of the dynamic feature to retrieve the definition for.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The dynamic feature's definition.</returns>	
        Task<DynamicFeatureDefinition> GetDynamicFeatureDefinitionAsync(string dynamicFeatureName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves definitions for all dynamic features.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An enumerator which provides asynchronous iteration over dynamic feature definitions.</returns>
        IAsyncEnumerable<DynamicFeatureDefinition> GetAllDynamicFeatureDefinitionsAsync(CancellationToken cancellationToken = default);
    }
}
