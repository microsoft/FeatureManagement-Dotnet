// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    public interface IFilterParametersCache
    {
        FilterParametersCacheItem Get(string cacheKey);

        void Set(string key, FilterParametersCacheItem cacheItem);
    }
}
