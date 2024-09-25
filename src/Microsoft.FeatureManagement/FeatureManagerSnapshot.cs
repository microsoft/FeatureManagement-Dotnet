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
        private readonly IFeatureManager _featureManager;
        private readonly IVariantFeatureManager _variantFeatureManager;
        private readonly ConcurrentDictionary<string, Task<bool>> _flagCache = new ConcurrentDictionary<string, Task<bool>>();
        private readonly ConcurrentDictionary<string, ValueTask<bool>> _variantFlagCache = new ConcurrentDictionary<string, ValueTask<bool>>();
        private readonly ConcurrentDictionary<string, Variant> _variantCache = new ConcurrentDictionary<string, Variant>();
        private IEnumerable<string> _featureNames;

        // Takes both a feature manager and a variant feature manager for backwards compatibility.
        public FeatureManagerSnapshot(IFeatureManager featureManager, IVariantFeatureManager variantFeatureManager)
        {
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
            _variantFeatureManager = variantFeatureManager ?? throw new ArgumentNullException(nameof(variantFeatureManager));
        }

        public async IAsyncEnumerable<string> GetFeatureNamesAsync()
        {
            if (_featureNames == null)
            {
                var featureNames = new List<string>();

                await foreach (string featureName in _featureManager.GetFeatureNamesAsync().ConfigureAwait(false))
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

        public async IAsyncEnumerable<string> GetFeatureNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (_featureNames == null)
            {
                var featureNames = new List<string>();

                await foreach (string featureName in _variantFeatureManager.GetFeatureNamesAsync(cancellationToken).ConfigureAwait(false))
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
                (key) => _featureManager.IsEnabledAsync(key));
        }

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext context)
        {
            return _flagCache.GetOrAdd(
                feature,
                (key) => _featureManager.IsEnabledAsync(key, context));
        }

        public ValueTask<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken)
        {
            return _variantFlagCache.GetOrAdd(
                feature,
                (key) => _variantFeatureManager.IsEnabledAsync(key, cancellationToken));
        }

        public ValueTask<bool> IsEnabledAsync<TContext>(string feature, TContext context, CancellationToken cancellationToken)
        {
            return _variantFlagCache.GetOrAdd(
                feature,
                (key) => _variantFeatureManager.IsEnabledAsync(key, context, cancellationToken));
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

            Variant variant = await _variantFeatureManager.GetVariantAsync(feature, cancellationToken).ConfigureAwait(false);

            _variantCache[cacheKey] = variant;

            return variant;
        }

        public async ValueTask<Variant> GetVariantAsync(string feature, ITargetingContext context, CancellationToken cancellationToken)
        {
            string cacheKey = GetVariantCacheKey(feature);

            //
            // First, check local cache
            if (_variantCache.ContainsKey(feature))
            {
                return _variantCache[cacheKey];
            }

            Variant variant = await _variantFeatureManager.GetVariantAsync(feature, context, cancellationToken).ConfigureAwait(false);

            _variantCache[cacheKey] = variant;

            return variant;
        }

        private string GetVariantCacheKey(string feature)
        {
            return $"{typeof(Variant).FullName}\n{feature}";
        }
    }
}
