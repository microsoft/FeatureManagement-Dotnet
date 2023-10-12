// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Cache of feature filter parameters and bound settings.
    /// Parameters of <see cref="IFeatureFilter"/>s which implement <see cref="IFilterParametersBinder"/> can be cached by the feature management system.
    /// </summary>
    public interface IFilterParametersCache
    {
        /// <summary>
        /// Get the <see cref="FilterParametersCacheItem"/> through the cache key
        /// </summary>
        /// <param name="cacheKey">cache key.</param>
        FilterParametersCacheItem Get(string cacheKey);

        /// <summary>
        /// Set the <see cref="FilterParametersCacheItem"/> with the cache key
        /// </summary>
        /// <param name="cacheKey">cache key.</param>
        /// <param name="cacheItem">cache item.</param>
        void Set(string cacheKey, FilterParametersCacheItem cacheItem);
    }
}
