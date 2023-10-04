// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A provider of feature definitions.
    /// </summary>
    public interface IFeatureDefinitionProvider
    {
        /// <summary>
        /// Retrieves the definition for a given feature.
        /// </summary>
        /// <param name="featureName">The name of the feature to retrieve the definition for.</param>
        /// <returns>The feature's definition.</returns>	
        Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName);

        /// <summary>
        /// Retrieves definitions for all features.
        /// </summary>
        /// <returns>An enumerator which provides asynchronous iteration over feature definitions.</returns>
        IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync();
    }
}
