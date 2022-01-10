// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A feature definition provider that pulls dynamic feature definitions from the .NET Core <see cref="IConfiguration"/> system.
    /// </summary>
    sealed class ConfigurationDynamicFeatureDefinitionProvider : IDynamicFeatureDefinitionProvider, IDisposable
    {
        private const string FeatureManagementSectionName = "FeatureManagement";
        private const string DynamicFeatureDefinitionsSectionName= "DynamicFeatures";
        private const string FeatureVariantsSectionName = "Variants";
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, DynamicFeatureDefinition> _dynamicFeatureDefinitions;
        private IDisposable _changeSubscription;
        private int _stale = 0;

        public ConfigurationDynamicFeatureDefinitionProvider(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dynamicFeatureDefinitions = new ConcurrentDictionary<string, DynamicFeatureDefinition>();

            _changeSubscription = ChangeToken.OnChange(
                () => _configuration.GetReloadToken(),
                () => _stale = 1);
        }

        public void Dispose()
        {
            _changeSubscription?.Dispose();

            _changeSubscription = null;
        }

        public Task<DynamicFeatureDefinition> GetDynamicFeatureDefinitionAsync(string featureName, CancellationToken cancellationToken = default)
        {
            if (featureName == null)
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            EnsureFresh();

            //
            // Query by feature name
            DynamicFeatureDefinition definition = _dynamicFeatureDefinitions.GetOrAdd(featureName, (name) => ReadDynamicFeatureDefinition(name));

            return Task.FromResult(definition);
        }

        //
        // The async key word is necessary for creating IAsyncEnumerable.
        // The need to disable this warning occurs when implementaing async stream synchronously. 
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerable<DynamicFeatureDefinition> GetAllDynamicFeatureDefinitionsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            EnsureFresh();

            //
            // Iterate over all features registered in the system at initial invocation time
            foreach (IConfigurationSection featureSection in GetDynamicFeatureDefinitionSections())
            {
                //
                // Underlying IConfigurationSection data is dynamic so latest feature definitions are returned
                yield return _dynamicFeatureDefinitions.GetOrAdd(featureSection.Key, (_) => ReadDynamicFeatureDefinition(featureSection));
            }
        }

        private DynamicFeatureDefinition ReadDynamicFeatureDefinition(string featureName)
        {
            IConfigurationSection configuration = GetDynamicFeatureDefinitionSections()
                                                    .FirstOrDefault(section => section.Key.Equals(featureName, StringComparison.OrdinalIgnoreCase));

            if (configuration == null)
            {
                return null;
            }

            return ReadDynamicFeatureDefinition(configuration);
        }

        private DynamicFeatureDefinition ReadDynamicFeatureDefinition(IConfigurationSection configurationSection)
        {
            Debug.Assert(configurationSection != null);

            var variants = new List<FeatureVariant>();

            foreach (IConfigurationSection section in configurationSection.GetSection(FeatureVariantsSectionName).GetChildren())
            {
                if (int.TryParse(section.Key, out int _) && !string.IsNullOrEmpty(section[nameof(FeatureVariant.Name)]))
                {
                    variants.Add(new FeatureVariant
                    {
                        Default = section.GetValue<bool>(nameof(FeatureVariant.Default)),
                        Name = section.GetValue<string>(nameof(FeatureVariant.Name)),
                        ConfigurationReference = section.GetValue<string>(nameof(FeatureVariant.ConfigurationReference)),
                        AssignmentParameters = section.GetSection(nameof(FeatureVariant.AssignmentParameters))
                    });
                }
            }

            return new DynamicFeatureDefinition()
            {
                Name = configurationSection.Key,
                Variants = variants,
                Assigner = configurationSection.GetValue<string>(nameof(DynamicFeatureDefinition.Assigner))
            };
        }
        
        private IEnumerable<IConfigurationSection> GetDynamicFeatureDefinitionSections()
        {
            //
            // Look for feature definitions under the "FeatureManagement" section
            IConfiguration featureManagementSection = _configuration.GetChildren().Any(s => s.Key.Equals(FeatureManagementSectionName, StringComparison.OrdinalIgnoreCase)) ?
                _configuration.GetSection(FeatureManagementSectionName) :
                _configuration;

            return featureManagementSection
                .GetSection(DynamicFeatureDefinitionsSectionName)
                .GetChildren();
        }

        private void EnsureFresh()
        {
            if (Interlocked.Exchange(ref _stale, 0) != 0)
            {
                _dynamicFeatureDefinitions.Clear();
            }
        }
    }
}
