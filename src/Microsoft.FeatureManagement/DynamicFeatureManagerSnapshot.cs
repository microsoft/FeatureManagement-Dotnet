// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a snapshot of feature state to ensure consistency across a given request.
    /// </summary>
    class DynamicFeatureManagerSnapshot : IDynamicFeatureManagerSnapshot
    {
        private readonly IDynamicFeatureManager _dynamicFeatureManager;
        private readonly IDictionary<string, object> _variantCache = new Dictionary<string, object>();
        private IEnumerable<string> _dynamicFeatureNames;

        public DynamicFeatureManagerSnapshot(IDynamicFeatureManager dynamicFeatureManager)
        {
            _dynamicFeatureManager = dynamicFeatureManager ?? throw new ArgumentNullException(nameof(dynamicFeatureManager));
        }

        public async IAsyncEnumerable<string> GetDynamicFeatureNamesAsync([EnumeratorCancellation]CancellationToken cancellationToken = default)
        {
            if (_dynamicFeatureNames == null)
            {
                var dynamicFeatureNames = new List<string>();

                await foreach (string featureName in _dynamicFeatureManager.GetDynamicFeatureNamesAsync(cancellationToken).ConfigureAwait(false))
                {
                    dynamicFeatureNames.Add(featureName);
                }

                _dynamicFeatureNames = dynamicFeatureNames;
            }

            foreach (string featureName in _dynamicFeatureNames)
            {
                yield return featureName;
            }
        }

        public async ValueTask<T> GetVariantAsync<T>(string feature, CancellationToken cancellationToken)
        {
            string cacheKey = GetVariantCacheKey<T>(feature);

            //
            // First, check local cache
            if (_variantCache.ContainsKey(feature))
            {
                return (T)_variantCache[cacheKey];
            }

            T variant = await _dynamicFeatureManager.GetVariantAsync<T>(feature, cancellationToken).ConfigureAwait(false);

            _variantCache[cacheKey] = variant;

            return variant;
        }

        public async ValueTask<T> GetVariantAsync<T, TContext>(string feature, TContext context, CancellationToken cancellationToken)
        {
            string cacheKey = GetVariantCacheKey<T>(feature);

            //
            // First, check local cache
            if (_variantCache.ContainsKey(feature))
            {
                return (T)_variantCache[cacheKey];
            }

            T variant = await _dynamicFeatureManager.GetVariantAsync<T, TContext>(feature, context, cancellationToken).ConfigureAwait(false);

            _variantCache[cacheKey] = variant;

            return variant;
        }

        private string GetVariantCacheKey<T>(string feature)
        {
            return $"{typeof(T).FullName}\n{feature}";
        }
    }
}
