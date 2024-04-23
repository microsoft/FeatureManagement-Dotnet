using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.FeatureManagement;

namespace Tests.FeatureManagement
{
    class TestSessionManager : ISessionManager
    {
        private readonly IDictionary<string, bool> _features = new Dictionary<string, bool>();
        
        public Task SetAsync(string featureName, bool enabled)
        {
            _features[featureName] = enabled;

            return Task.CompletedTask;
        }

        public Task<bool?> GetAsync(string featureName)
        {
            bool? result = _features.TryGetValue(featureName, out var feature) ? feature : null;

            return Task.FromResult(result);
        }
    }
}