using Microsoft.FeatureManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    class InMemoryFeatureDefinitionProvider : IFeatureDefinitionProvider, IFeatureDefinitionProviderCacheable
    {
        private IEnumerable<FeatureDefinition> _definitions;

        public InMemoryFeatureDefinitionProvider(IEnumerable<FeatureDefinition> featureDefinitions)
        {
            _definitions = featureDefinitions ?? throw new ArgumentNullException(nameof(featureDefinitions));
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            foreach (FeatureDefinition definition in _definitions)
            {
                yield return definition;
            }
        }

        public Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            return Task.FromResult(_definitions.FirstOrDefault(definitions => definitions.Name.Equals(featureName, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
