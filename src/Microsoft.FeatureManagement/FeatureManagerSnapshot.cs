// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a snapshot of feature state to ensure consistency across a given request.
    /// </summary>
    class FeatureManagerSnapshot : IFeatureManagerSnapshot
    {
        private readonly IFeatureManager _featureManager;
        private readonly IDictionary<string, bool> _flagCache = new Dictionary<string, bool>();

        public FeatureManagerSnapshot(IFeatureManager featureManager)
        {
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
        }

        public async Task<bool> IsEnabledAsync(string feature)
        {
            //
            // First, check local cache
            if (_flagCache.ContainsKey(feature))
            {
                return _flagCache[feature];
            }

            bool enabled = await _featureManager.IsEnabledAsync(feature).ConfigureAwait(false);

            _flagCache[feature] = enabled;

            return enabled;
        }

        public async Task<bool> IsEnabledAsync<TContext>(string feature, TContext context)
        {
            //
            // First, check local cache
            if (_flagCache.ContainsKey(feature))
            {
                return _flagCache[feature];
            }

            bool enabled = await _featureManager.IsEnabledAsync(feature, context).ConfigureAwait(false);

            _flagCache[feature] = enabled;

            return enabled;
        }
    }
}
