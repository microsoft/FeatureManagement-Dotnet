// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Caching.Memory;
using System;

namespace Microsoft.FeatureManagement
{

    /// <summary>
    /// Provides a cache for feature filter parameters and bound settings.
    /// </summary>
    class FilterParametersCache : IFilterParametersCache, IDisposable
    {
        private readonly TimeSpan ParametersCacheSlidingExpiration = TimeSpan.FromMinutes(5);
        private readonly TimeSpan ParametersCacheAbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

        private readonly IMemoryCache _parametersCache;

        public FilterParametersCache()
        {
            _parametersCache = new MemoryCache(new MemoryCacheOptions());
        }

        public void Dispose()
        {
            _parametersCache.Dispose();
        }

        public FilterParametersCacheItem Get(string cacheKey)
        {
            FilterParametersCacheItem cacheItem;

            if (_parametersCache.TryGetValue(cacheKey, out cacheItem))
            {
                return cacheItem;
            }

            return null;
        }

        public void Set(string cacheKey, FilterParametersCacheItem cacheItem)
        {
            _parametersCache.Set(
            cacheKey,
            cacheItem,
            new MemoryCacheEntryOptions
            {
                SlidingExpiration = ParametersCacheSlidingExpiration,
                AbsoluteExpirationRelativeToNow = ParametersCacheAbsoluteExpirationRelativeToNow
            });
        }
    }
}
