// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    class TestSessionManager : ISessionManager
    {
        private readonly ConcurrentDictionary<string, bool> _session = new ConcurrentDictionary<string, bool>();

        public Task SetAsync(string featureName, bool enabled)
        {
            _session[featureName] = enabled;

            return Task.CompletedTask;
        }

        public Task<bool?> GetAsync(string featureName)
        {
            if (_session.TryGetValue(featureName, out bool enabled))
            {
                return Task.FromResult<bool?>(enabled);
            }

            return Task.FromResult<bool?>(null);
        }
    }
}
