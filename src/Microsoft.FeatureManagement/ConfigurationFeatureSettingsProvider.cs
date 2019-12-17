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
    /// A feature settings provider that pulls settings from the .NET Core <see cref="IConfiguration"/> system.
    /// </summary>
    sealed class ConfigurationFeatureSettingsProvider : IFeatureSettingsProvider, IDisposable
    {
        private const string FeatureFiltersSectionName = "EnabledFor";
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, FeatureSettings> _settings;
        private IDisposable _changeSubscription;
        private int _stale = 0;

        public ConfigurationFeatureSettingsProvider(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _settings = new ConcurrentDictionary<string, FeatureSettings>();

            _changeSubscription = ChangeToken.OnChange(
                () => _configuration.GetReloadToken(),
                () => _stale = 1);
        }

        public void Dispose()
        {
            _changeSubscription?.Dispose();

            _changeSubscription = null;
        }

        public Task<IEnumerable<FeatureSettings>> GetFeatureSettings(FeatureSettingsQueryOptions queryOptions)
        {
            if (queryOptions == null)
            {
                throw new ArgumentNullException(nameof(queryOptions));
            }

            if (Interlocked.Exchange(ref _stale, 0) != 0)
            {
                _settings.Clear();
            }

            var featureSettings = new List<FeatureSettings>();

            if (queryOptions.FeatureName != null)
            {
                //
                // Query by feature name
                FeatureSettings settings = _settings.GetOrAdd(queryOptions.FeatureName, (name) => ReadFeatureSettings(name));

                if (settings != null)
                {
                    featureSettings.Add(settings);
                }
            }
            else
            {
                //
                // Query all
                foreach (string featureName in GetFeatureConfigurationSections().Select(s => s.Key))
                {
                    if (!string.IsNullOrEmpty(queryOptions.After) && string.Compare(featureName, queryOptions.After) <= 0)
                    {
                        continue;
                    }

                    FeatureSettings settings = _settings.GetOrAdd(featureName, (name) => ReadFeatureSettings(name));

                    if (settings != null)
                    {
                        featureSettings.Add(settings);
                    }
                }
            }

            return Task.FromResult<IEnumerable<FeatureSettings>>(featureSettings);
        }

        private FeatureSettings ReadFeatureSettings(string featureName)
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

            IConfigurationSection configuration = GetFeatureConfigurationSections()
                                                    .FirstOrDefault(section => section.Key.Equals(featureName, StringComparison.OrdinalIgnoreCase));

            if (configuration == null)
            {
                return null;
            }

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
