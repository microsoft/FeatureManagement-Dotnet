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
        /// <param name="queryOptions">Options specifying what feature settings should be retrieved.</param>
        /// <returns>A list of feature settings matching the provided query options.</returns>
        Task<IEnumerable<FeatureSettings>> GetFeatureSettings(FeatureSettingsQueryOptions queryOptions);
    }
}
