// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A provider of feature settings.
    /// </summary>
    interface IFeatureSettingsProvider
    {
        /// <summary>
        /// Retrieves settings for a given feature.
        /// </summary>
        /// <param name="featureName">The name of the feature to retrieve settings for.</param>
        /// <returns>The feature's settings.</returns>	
        Task<FeatureSettings> GetFeatureSettingsAsync(string featureName);

        /// <summary>
        /// Retrieves settings for all features.
        /// </summary>
        /// <returns>An enumerator which provides asynchronous iteration over feature settings.</returns>
        IAsyncEnumerable<FeatureSettings> GetAllFeatureSettingsAsync();
    }
}
