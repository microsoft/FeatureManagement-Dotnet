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

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A feature settings provider that pulls settings from the .NET Core <see cref="IConfiguration"/> system.
    /// </summary>
    sealed class ConfigurationFeatureSettingsProvider : IFeatureSettingsProvider, IDisposable
    {
        private const string FeatureFiltersSectionName = "EnabledFor";
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, IFeatureSettings> _settings;
        private IDisposable _changeSubscription;
        private int _stale = 0;

        public ConfigurationFeatureSettingsProvider(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _settings = new ConcurrentDictionary<string, IFeatureSettings>();

            _changeSubscription = ChangeToken.OnChange(
                () => _configuration.GetReloadToken(),
                () => _stale = 1);
        }

        public void Dispose()
        {
            _changeSubscription?.Dispose();

            _changeSubscription = null;
        }

        public IFeatureSettings TryGetFeatureSettings(string featureName)
        {
            if (Interlocked.Exchange(ref _stale, 0) != 0)
            {
                _settings.Clear();
            }

            return _settings.GetOrAdd(featureName, (name) => ReadFeatureSettings(name));
        }

        private IFeatureSettings ReadFeatureSettings(string featureName)
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

            IConfigurationSection configuration = GetFeatureConfiguration(featureName);

            var enabledFor = new List<FeatureFilterSettings>();

            string val = configuration.Value; // configuration[$"{featureName}"];

            if (string.IsNullOrEmpty(val))
            {
                val = configuration[FeatureFiltersSectionName];
            }

            if (!string.IsNullOrEmpty(val) && bool.TryParse(val, out bool result) && result)
            {
                //
                //myAlwaysEnabledFeature: true
                // OR
                //myAlwaysEnabledFeature: {
                //  enabledFor: true
                //}
                enabledFor.Add(new FeatureFilterSettings
                {
                    Name = "AlwaysOn"
                });
            }
            else
            {
                IEnumerable<IConfigurationSection> filterSections = configuration.GetSection(FeatureFiltersSectionName).GetChildren();

                foreach (IConfigurationSection section in filterSections)
                {
                    //
                    // Arrays in json such as "myKey": [ "some", "values" ]
                    // Are accessed through the configuration system by using the array index as the property name, e.g. "myKey": { "0": "some", "1": "values" }
                    if (int.TryParse(section.Key, out int i) && !string.IsNullOrEmpty(section[nameof(FeatureFilterSettings.Name)]))
                    {
                        enabledFor.Add(new FeatureFilterSettings()
                        {
                            Name = section[nameof(FeatureFilterSettings.Name)],
                            Parameters = section.GetSection(nameof(FeatureFilterSettings.Parameters))
                        });
                    }
                }
            }

            return new FeatureSettings()
            {
                Name = featureName,
                EnabledFor = enabledFor
            };
        }

        private IConfigurationSection GetFeatureConfiguration(string featureName)
        {
            const string FeatureManagementSectionName = "FeatureManagement";

            //
            // Look for settings under the "FeatureManagement" section
            IConfigurationSection featureConfiguration = _configuration.GetSection(FeatureManagementSectionName).GetChildren().FirstOrDefault(section => section.Key.Equals(featureName, StringComparison.OrdinalIgnoreCase));

            //
            // Fallback to the configuration section using the feature's name
            if (featureConfiguration == null)
            {
                featureConfiguration = _configuration.GetSection(featureName);
            }

            return featureConfiguration;
        }
    }
}
