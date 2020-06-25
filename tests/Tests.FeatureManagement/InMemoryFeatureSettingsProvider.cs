using Microsoft.FeatureManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    class InMemoryFeatureSettingsProvider : IFeatureDefinitionProvider
    {
        private IEnumerable<FeatureDefinition> _settings;

        public InMemoryFeatureSettingsProvider(IEnumerable<FeatureDefinition> featureSettings)
        {
            _settings = featureSettings ?? throw new ArgumentNullException(nameof(featureSettings));
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            foreach (FeatureDefinition settings in _settings)
            {
                yield return settings;
            }
        }

        public Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            return Task.FromResult(_settings.FirstOrDefault(settings => settings.Name.Equals(featureName, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
