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
    class FeatureManagerSnapshot : IFeatureManagerSnapshot, IDynamicFeatureManagerSnapshot
    {
        private readonly IFeatureManager _featureManager;
        private readonly IDynamicFeatureManager _dynamicFeatureManager;
        private readonly IDictionary<string, bool> _flagCache = new Dictionary<string, bool>();
        private readonly IDictionary<string, object> _variantCache = new Dictionary<string, object>();
        private IEnumerable<string> _featureFlagNames;
        private IEnumerable<string> _dynamicFeatureNames;

        public FeatureManagerSnapshot(
            IFeatureManager featureManager,
            IDynamicFeatureManager dynamicFeatureManager)
        {
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
            _dynamicFeatureManager = dynamicFeatureManager ?? throw new ArgumentNullException(nameof(dynamicFeatureManager));
        }

        public async IAsyncEnumerable<string> GetFeatureFlagNamesAsync([EnumeratorCancellation]CancellationToken cancellationToken)
        {
            if (_featureFlagNames == null)
            {
                var featureNames = new List<string>();

                await foreach (string featureName in _featureManager.GetFeatureFlagNamesAsync(cancellationToken).ConfigureAwait(false))
                {
                    featureNames.Add(featureName);
                }

                _featureFlagNames = featureNames;
            }

            foreach (string featureName in _featureFlagNames)
            {
                yield return featureName;
            }
        }

        public async IAsyncEnumerable<string> GetDynamicFeatureNamesAsync([EnumeratorCancellation]CancellationToken cancellationToken = default)
        {
            if (_dynamicFeatureNames == null)
            {
                var dynamicFeatureNames = new List<string>();

                await foreach (string featureName in _featureManager.GetFeatureFlagNamesAsync(cancellationToken).ConfigureAwait(false))
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
            if (_flagCache.ContainsKey(feature))
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
            if (_flagCache.ContainsKey(feature))
            {
                return (T)_variantCache[cacheKey];
            }

            T variant = await _dynamicFeatureManager.GetVariantAsync<T>(feature, cancellationToken).ConfigureAwait(false);

            _variantCache[cacheKey] = variant;

            return variant;
        }

        public async Task<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken)
        {
            //
            // First, check local cache
            if (_flagCache.ContainsKey(feature))
            {
                return _flagCache[feature];
            }

            bool enabled = await _featureManager.IsEnabledAsync(feature, cancellationToken).ConfigureAwait(false);

            _flagCache[feature] = enabled;

            return enabled;
        }

        public async Task<bool> IsEnabledAsync<TContext>(string feature, TContext context, CancellationToken cancellationToken)
        {
            //
            // First, check local cache
            if (_flagCache.ContainsKey(feature))
            {
                return _flagCache[feature];
            }

            bool enabled = await _featureManager.IsEnabledAsync(feature, context, cancellationToken).ConfigureAwait(false);

            _flagCache[feature] = enabled;

            return enabled;
        }

        private string GetVariantCacheKey<T>(string feature)
        {
            return $"{typeof(T).FullName}\n{feature}";
        }
    }
}
