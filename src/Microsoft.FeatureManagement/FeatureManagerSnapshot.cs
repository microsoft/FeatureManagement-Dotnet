// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a snapshot of feature state to ensure consistency across a given request.
    /// </summary>
    class FeatureManagerSnapshot : IFeatureManagerSnapshot, IVariantFeatureManagerSnapshot
    {
        private readonly IVariantFeatureManager _featureManager;
        private readonly ConcurrentDictionary<string, ValueTask<bool>> _flagCache = new ConcurrentDictionary<string, ValueTask<bool>>();
        private readonly ConcurrentDictionary<string, Variant> _variantCache = new ConcurrentDictionary<string, Variant>();
        private IEnumerable<string> _featureNames;

        public FeatureManagerSnapshot(IVariantFeatureManager featureManager)
        {
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
        }

        public IAsyncEnumerable<string> GetFeatureNamesAsync()
        {
            return GetFeatureNamesAsync(CancellationToken.None);
        }

        public async IAsyncEnumerable<string> GetFeatureNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (_featureNames == null)
            {
                var featureNames = new List<string>();

                await foreach (string featureName in _featureManager.GetFeatureNamesAsync(cancellationToken).ConfigureAwait(false))
                {
                    featureNames.Add(featureName);
                }

                _featureNames = featureNames;
            }

            foreach (string featureName in _featureNames)
            {
                yield return featureName;
            }
        }

        public Task<bool> IsEnabledAsync(string feature)
        {
            return _flagCache.GetOrAdd(
                feature,
                (key) => _featureManager.IsEnabledAsync(key, CancellationToken.None)).AsTask();
        }

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext context)
        {
            return _flagCache.GetOrAdd(
                feature,
                (key) => _featureManager.IsEnabledAsync(key, context, CancellationToken.None)).AsTask();
        }

        public ValueTask<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken)
        {
            return _flagCache.GetOrAdd(
                feature,
                (key) => _featureManager.IsEnabledAsync(key, cancellationToken));
        }

        public ValueTask<bool> IsEnabledAsync<TContext>(string feature, TContext context, CancellationToken cancellationToken)
        {
            return _flagCache.GetOrAdd(
                feature,
                (key) => _featureManager.IsEnabledAsync(key, context, cancellationToken));
        }

        public async ValueTask<Variant> GetVariantAsync(string feature, CancellationToken cancellationToken)
        {
            string cacheKey = GetVariantCacheKey(feature);

            //
            // First, check local cache
            if (_variantCache.ContainsKey(feature))
            {
                return _variantCache[cacheKey];
            }

            Variant variant = await _featureManager.GetVariantAsync(feature, cancellationToken).ConfigureAwait(false);

            _variantCache[cacheKey] = variant;

            return variant;
        }

        public async ValueTask<Variant> GetVariantAsync(string feature, TargetingContext context, CancellationToken cancellationToken)
        {
            string cacheKey = GetVariantCacheKey(feature);

            //
            // First, check local cache
            if (_variantCache.ContainsKey(feature))
            {
                return _variantCache[cacheKey];
            }

            Variant variant = await _featureManager.GetVariantAsync(feature, context, cancellationToken).ConfigureAwait(false);

            _variantCache[cacheKey] = variant;

            return variant;
        }

        private string GetVariantCacheKey(string feature)
        {
            return $"{typeof(Variant).FullName}\n{feature}";
        }
    }
}
