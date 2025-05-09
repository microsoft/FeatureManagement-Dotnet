// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Caching.Memory;

namespace Tests.FeatureManagement
{
    class TestCache : IMemoryCache
    {
        private readonly IMemoryCache _cache;
        private int _countOfEntryCreation;

        public TestCache()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        public int CountOfEntryCreation
        {
            get => _countOfEntryCreation;
        }

        public bool TryGetValue(object key, out object value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public ICacheEntry CreateEntry(object key)
        {
            _countOfEntryCreation += 1;

            return _cache.CreateEntry(key);
        }

        public void Remove(object key)
        {
            _cache.Remove(key);
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
