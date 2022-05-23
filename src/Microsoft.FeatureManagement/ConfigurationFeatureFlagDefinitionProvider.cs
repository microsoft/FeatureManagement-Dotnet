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
    /// A feature definition provider that pulls feature flag definitions from the .NET Core <see cref="IConfiguration"/> system.
    /// </summary>
    sealed class ConfigurationFeatureFlagDefinitionProvider : IFeatureFlagDefinitionProvider, IDisposable
    {
        private const string FeatureManagementSectionName = "FeatureManagement";
        private const string FeatureFlagDefinitionsSectionName = "FeatureFlags";
        private const string FeatureFiltersSectionName = "EnabledFor";
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, FeatureFlagDefinition> _featureFlagDefinitions;
        private IDisposable _changeSubscription;
        private int _stale = 0;

        public ConfigurationFeatureFlagDefinitionProvider(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _featureFlagDefinitions = new ConcurrentDictionary<string, FeatureFlagDefinition>();

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
                            //
                            // Use wrapper to insure new reference for caching support
                            Parameters = new ConfigurationWrapper(section.GetSection(nameof(FeatureFilterConfiguration.Parameters)))
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

        private IEnumerable<IConfigurationSection> GetFeatureFlagDefinitionSections()
        {
            //
            // Look for feature definitions under the "FeatureManagement" section
            IConfiguration featureManagementSection = _configuration.GetChildren().Any(s => s.Key.Equals(FeatureManagementSectionName, StringComparison.OrdinalIgnoreCase)) ?
                _configuration.GetSection(FeatureManagementSectionName) :
                _configuration;

            IEnumerable<IConfigurationSection> featureManagementChildren = featureManagementSection.GetChildren();

            IConfigurationSection featureFlagsSection = featureManagementChildren.FirstOrDefault(s => s.Key == FeatureFlagDefinitionsSectionName);

            //
            // Support backward compatability where feature flag definitions were directly under the feature management section
            return featureFlagsSection == null ?
                featureManagementChildren :
                featureFlagsSection.GetChildren();
        }

        private void EnsureFresh()
        {
            if (Interlocked.Exchange(ref _stale, 0) != 0)
            {
                _featureFlagDefinitions.Clear();
            }
        }
    }
}
