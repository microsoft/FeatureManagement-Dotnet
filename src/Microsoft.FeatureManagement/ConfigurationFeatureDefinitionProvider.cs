// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A feature definition provider that pulls settings from the .NET Core <see cref="IConfiguration"/> system.
    /// </summary>
    sealed class ConfigurationFeatureDefinitionProvider : IFeatureDefinitionProvider, IDisposable
    {
        private const string FeatureFiltersSectionName = "EnabledFor";
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, FeatureDefinition> _settings;
        private IDisposable _changeSubscription;
        private int _stale = 0;

        public ConfigurationFeatureDefinitionProvider(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _settings = new ConcurrentDictionary<string, FeatureDefinition>();

            _changeSubscription = ChangeToken.OnChange(
                () => _configuration.GetReloadToken(),
                () => _stale = 1);
        }

        public void Dispose()
        {
            _changeSubscription?.Dispose();

            _changeSubscription = null;
        }

        public Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            if (featureName == null)
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            if (Interlocked.Exchange(ref _stale, 0) != 0)
            {
                _settings.Clear();
            }

            //
            // Query by feature name
            FeatureDefinition settings = _settings.GetOrAdd(featureName, (name) => ReadFeatureSettings(name));

            return Task.FromResult(settings);
        }

        //
        // The async key word is necessary for creating IAsyncEnumerable.
        // The need to disable this warning occurs when implementaing async stream synchronously. 
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
#pragma warning restore CS1998
        {
            if (Interlocked.Exchange(ref _stale, 0) != 0)
            {
                _settings.Clear();
            }

            //
            // Iterate over all features registered in the system at initial invocation time
            foreach (IConfigurationSection featureSection in GetFeatureConfigurationSections())
            {
                //
                // Underlying IConfigurationSection data is dynamic so latest feature settings are returned
                yield return  _settings.GetOrAdd(featureSection.Key, (_) => ReadFeatureSettings(featureSection));
            }
        }

        private FeatureDefinition ReadFeatureSettings(string featureName)
        {
            IConfigurationSection configuration = GetFeatureConfigurationSections()
                                                    .FirstOrDefault(section => section.Key.Equals(featureName, StringComparison.OrdinalIgnoreCase));

            if (configuration == null)
            {
                return null;
            }

            return ReadFeatureSettings(configuration);
        }

        private FeatureDefinition ReadFeatureSettings(IConfigurationSection configurationSection)
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
                    if (int.TryParse(section.Key, out int i) && !string.IsNullOrEmpty(section[nameof(FeatureFilterConfiguration.Name)]))
                    {
                        enabledFor.Add(new FeatureFilterConfiguration()
                        {
                            Name = section[nameof(FeatureFilterConfiguration.Name)],
                            Parameters = section.GetSection(nameof(FeatureFilterConfiguration.Parameters))
                        });
                    }
                }
            }

            return new FeatureDefinition()
            {
                Name = configurationSection.Key,
                EnabledFor = enabledFor
            };
        }

        private IEnumerable<IConfigurationSection> GetFeatureConfigurationSections()
        {
            const string FeatureManagementSectionName = "FeatureManagement";

            if (_configuration.GetChildren().Any(s => s.Key.Equals(FeatureManagementSectionName, StringComparison.OrdinalIgnoreCase)))
            {
                //
                // Look for settings under the "FeatureManagement" section
                return _configuration.GetSection(FeatureManagementSectionName).GetChildren();
            }
            else
            {
                return _configuration.GetChildren();
            }
        }
    }
}
