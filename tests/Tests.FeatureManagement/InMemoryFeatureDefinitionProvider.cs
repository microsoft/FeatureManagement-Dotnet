// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    class InMemoryFeatureDefinitionProvider : IFeatureFlagDefinitionProvider, IDynamicFeatureDefinitionProvider
    {
        private IEnumerable<FeatureFlagDefinition> _featureFlagDefinitions;
        private IEnumerable<DynamicFeatureDefinition> _dynamicFeatureDefinitions;

        public InMemoryFeatureDefinitionProvider(
            IEnumerable<FeatureFlagDefinition> featureFlagDefinitions,
            IEnumerable<DynamicFeatureDefinition> dynamicFeatureDefinitions)
        {
            _featureFlagDefinitions = featureFlagDefinitions ?? throw new ArgumentNullException(nameof(featureFlagDefinitions));
            _dynamicFeatureDefinitions = dynamicFeatureDefinitions ?? throw new ArgumentNullException(nameof(dynamicFeatureDefinitions));
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerable<FeatureFlagDefinition> GetAllFeatureFlagDefinitionsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            foreach (FeatureFlagDefinition definition in _featureFlagDefinitions)
            {
                yield return definition;
            }
        }

        public Task<FeatureFlagDefinition> GetFeatureFlagDefinitionAsync(string featureName, CancellationToken cancellationToken)
        {
            return Task.FromResult(_featureFlagDefinitions.FirstOrDefault(definitions => definitions.Name.Equals(featureName, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<DynamicFeatureDefinition> GetDynamicFeatureDefinitionAsync(string featureName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_dynamicFeatureDefinitions.FirstOrDefault(definitions => definitions.Name.Equals(featureName, StringComparison.OrdinalIgnoreCase)));
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerable<DynamicFeatureDefinition> GetAllDynamicFeatureDefinitionsAsync(CancellationToken cancellationToken = default)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            foreach (DynamicFeatureDefinition definition in _dynamicFeatureDefinitions)
            {
                yield return definition;
            }
        }
    }
}
