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
    /// A feature definition provider that pulls feature definitions from the .NET Core <see cref="IConfiguration"/> system.
    /// </summary>
    sealed class ConfigurationFeatureDefinitionProvider : IFeatureDefinitionProvider, IDisposable
    {
        private const string FeatureManagementSectionName = "FeatureManagement";
        private const string FeatureFlagDefinitionsSectionName = "FeatureFlags";
        private const string DynamicFeatureDefinitionsSectionName= "DynamicFeatures";
        private const string FeatureFiltersSectionName = "EnabledFor";
        private const string FeatureVariantsSectionName = "Variants";
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, FeatureFlagDefinition> _featureFlagDefinitions;
        private readonly ConcurrentDictionary<string, DynamicFeatureDefinition> _dynamicFeatureDefinitions;
        private IDisposable _changeSubscription;
        private int _stale = 0;

        public ConfigurationFeatureDefinitionProvider(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _featureFlagDefinitions = new ConcurrentDictionary<string, FeatureFlagDefinition>();
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

        public Task<FeatureFlagDefinition> GetFeatureFlagDefinitionAsync(string featureName, CancellationToken cancellationToken)
        {
            if (featureName == null)
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            EnsureFresh();

            //
            // Query by feature name
            FeatureFlagDefinition definition = _featureFlagDefinitions.GetOrAdd(featureName, (name) => ReadFeatureFlagDefinition(name));

            return Task.FromResult(definition);
        }

        //
        // The async key word is necessary for creating IAsyncEnumerable.
        // The need to disable this warning occurs when implementaing async stream synchronously. 
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerable<FeatureFlagDefinition> GetAllFeatureFlagDefinitionsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
#pragma warning restore CS1998
        {
            EnsureFresh();

            //
            // Iterate over all features registered in the system at initial invocation time
            foreach (IConfigurationSection featureSection in GetFeatureFlagDefinitionSections())
            {
                //
                // Underlying IConfigurationSection data is dynamic so latest feature definitions are returned
                yield return  _featureFlagDefinitions.GetOrAdd(featureSection.Key, (_) => ReadFeatureFlagDefinition(featureSection));
            }
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

        private FeatureFlagDefinition ReadFeatureFlagDefinition(string featureName)
        {
            IConfigurationSection configuration = GetFeatureFlagDefinitionSections()
                                                    .FirstOrDefault(section => section.Key.Equals(featureName, StringComparison.OrdinalIgnoreCase));

            if (configuration == null)
            {
                return null;
            }

            return ReadFeatureFlagDefinition(configuration);
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

        private FeatureFlagDefinition ReadFeatureFlagDefinition(IConfigurationSection configurationSection)
        {
            /*
              
            We support
            
            myFeature: {
              enabledFor: [ "myFeatureFilter1", "myFeatureFilter2" ]
            },
            myDisabledFeature: {
              enabledFor: [  ]
            },
            myFeature2: {
              enabledFor: "myFeatureFilter1;myFeatureFilter2"
            },
            myDisabledFeature2: {
              enabledFor: ""
            },
            myFeature3: "myFeatureFilter1;myFeatureFilter2",
            myDisabledFeature3: "",
            myAlwaysEnabledFeature: true,
            myAlwaysDisabledFeature: false // removing this line would be the same as setting it to false
            myAlwaysEnabledFeature2: {
              enabledFor: true
            },
            myAlwaysDisabledFeature2: {
              enabledFor: false
            }

            */

            Debug.Assert(configurationSection != null);

            var enabledFor = new List<FeatureFilterConfiguration>();

            string val = configurationSection.Value; // configuration[$"{featureName}"];

            if (string.IsNullOrEmpty(val))
            {
                val = configurationSection[FeatureFiltersSectionName];
            }

            if (!string.IsNullOrEmpty(val) && bool.TryParse(val, out bool result) && result)
            {
                //
                //myAlwaysEnabledFeature: true
                // OR
                //myAlwaysEnabledFeature: {
                //  enabledFor: true
                //}
                enabledFor.Add(new FeatureFilterConfiguration
                {
                    Name = "AlwaysOn"
                });
            }
            else
            {
                IEnumerable<IConfigurationSection> filterSections = configurationSection.GetSection(FeatureFiltersSectionName).GetChildren();

                foreach (IConfigurationSection section in filterSections)
                {
                    //
                    // Arrays in json such as "myKey": [ "some", "values" ]
                    // Are accessed through the configuration system by using the array index as the property name, e.g. "myKey": { "0": "some", "1": "values" }
                    if (int.TryParse(section.Key, out int _) && !string.IsNullOrEmpty(section[nameof(FeatureFilterConfiguration.Name)]))
                    {
                        enabledFor.Add(new FeatureFilterConfiguration
                        {
                            Name = section[nameof(FeatureFilterConfiguration.Name)],
                            Parameters = section.GetSection(nameof(FeatureFilterConfiguration.Parameters))
                        });
                    }
                }
            }

            return new FeatureFlagDefinition()
            {
                Name = configurationSection.Key,
                EnabledFor = enabledFor,
            };
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

        private IEnumerable<IConfigurationSection> GetFeatureFlagDefinitionSections()
        {
            IEnumerable<IConfigurationSection> featureManagementChildren = GetFeatureManagementSection().GetChildren();

            IConfigurationSection featureFlagsSection = featureManagementChildren.FirstOrDefault(s => s.Key == FeatureFlagDefinitionsSectionName);

            //
            // Support backward compatability where feature flag definitions were directly under the feature management section
            return featureFlagsSection == null ?
                featureManagementChildren :
                featureFlagsSection.GetChildren();
        }
        
        private IEnumerable<IConfigurationSection> GetDynamicFeatureDefinitionSections()
        {
            return GetFeatureManagementSection()
                .GetSection(DynamicFeatureDefinitionsSectionName)
                .GetChildren();
        }

        private IConfiguration GetFeatureManagementSection()
        {
            //
            // Look for feature definitions under the "FeatureManagement" section
            return _configuration.GetChildren().Any(s => s.Key.Equals(FeatureManagementSectionName, StringComparison.OrdinalIgnoreCase)) ?
                _configuration.GetSection(FeatureManagementSectionName) :
                _configuration;
        }

        private void EnsureFresh()
        {
            if (Interlocked.Exchange(ref _stale, 0) != 0)
            {
                _featureFlagDefinitions.Clear();
                _dynamicFeatureDefinitions.Clear();
            }
        }
    }
}
