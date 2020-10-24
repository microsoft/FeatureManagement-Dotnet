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
        private readonly Dictionary<string, Task<bool>> _flagCache = new Dictionary<string, Task<bool>>();
        private List<string> _featureNames;

        public FeatureManagerSnapshot(IFeatureManager featureManager)
        {
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
        }

        public async IAsyncEnumerable<string> GetFeatureNamesAsync()
        {
            if (_featureNames == null)
            {
                _featureNames = await CreateListAsync(_featureManager).ConfigureAwait(false);
            }

            foreach (string featureName in _featureNames)
            {
                yield return featureName;
            }

            static async ValueTask<List<string>> CreateListAsync(IFeatureManager featureManager)
            {
                var featureNames = new List<string>();

                await foreach (string featureName in featureManager.GetFeatureNamesAsync().ConfigureAwait(false))
                {
                    featureNames.Add(featureName);
                }

                return featureNames;
            }
        }

        public Task<bool> IsEnabledAsync(string feature)
        {
            if (_flagCache.TryGetValue(feature, out Task<bool> task))
            {
                return task;
            }

            return Core();

            Task<bool> Core()
            {
                lock (_flagCache)
                {
                    if (_flagCache.TryGetValue(feature, out task))
                    {
                        return task;
                    }

                    task = _featureManager.IsEnabledAsync(feature);
                    _flagCache.Add(feature, task);

                    return task;
                }
            }
        }

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext context)
        {
            if (_flagCache.TryGetValue(feature, out Task<bool> task))
            {
                return task;
            }

            return Core();

            Task<bool> Core()
            {
                lock (_flagCache)
                {
                    if (_flagCache.TryGetValue(feature, out task))
                    {
                        return task;
                    }

                    task = _featureManager.IsEnabledAsync(feature, context);
                    _flagCache.Add(feature, task);

                    return task;
                }
            }
        }
    }
}
