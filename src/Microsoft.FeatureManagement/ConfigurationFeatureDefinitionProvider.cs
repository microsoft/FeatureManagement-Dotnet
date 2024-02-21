// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    /// A feature definition provider that pulls feature definitions from the .NET Core <see cref="IConfiguration"/> system.
    /// </summary>
    public sealed class ConfigurationFeatureDefinitionProvider : IFeatureDefinitionProvider, IDisposable, IFeatureDefinitionProviderCacheable
    {
        //
        // IFeatureDefinitionProviderCacheable interface is only used to mark this provider as cacheable. This allows our test suite's
        // provider to be marked for caching as well.

        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, FeatureDefinition> _definitions;
        private IDisposable _changeSubscription;
        private int _stale = 0;
        private bool _microsoftFeatureFlagSchemaEnabled;

        /// <summary>
        /// Creates a configuration feature definition provider.
        /// </summary>
        /// <param name="configuration">The configuration of feature definitions.</param>
        public ConfigurationFeatureDefinitionProvider(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _definitions = new ConcurrentDictionary<string, FeatureDefinition>();

            _changeSubscription = ChangeToken.OnChange(
                () => _configuration.GetReloadToken(),
                () => _stale = 1);

            IConfiguration MicrosoftFeatureManagementConfigurationSection = _configuration
                .GetChildren()
                .FirstOrDefault(section =>
                    string.Equals(
                        section.Key,
                        MicrosoftFeatureManagementFields.FeatureManagementSectionName,
                        StringComparison.OrdinalIgnoreCase));

            if (MicrosoftFeatureManagementConfigurationSection != null)
            {
                _microsoftFeatureFlagSchemaEnabled = true;
            }
        }

        /// <summary>
        /// The option that controls the behavior when "FeatureManagement" section in the configuration is missing.
        /// </summary>
        public bool RootConfigurationFallbackEnabled { get; init; }

        /// <summary>
        /// The logger for the configuration feature definition provider.
        /// </summary>
        public ILogger Logger { get; init; }

        /// <summary>
        /// Disposes the change subscription of the configuration.
        /// </summary>
        public void Dispose()
        {
            _changeSubscription?.Dispose();

            _changeSubscription = null;
        }

        /// <summary>
        /// Retrieves the definition for a given feature.
        /// </summary>
        /// <param name="featureName">The name of the feature to retrieve the definition for.</param>
        /// <returns>The feature's definition.</returns>
        public Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            if (featureName == null)
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            if (featureName.Contains(ConfigurationPath.KeyDelimiter))
            {
                throw new ArgumentException($"The value '{ConfigurationPath.KeyDelimiter}' is not allowed in the feature name.", nameof(featureName));
            }

            if (Interlocked.Exchange(ref _stale, 0) != 0)
            {
                _definitions.Clear();
            }

            //
            // Query by feature name
            FeatureDefinition definition = _definitions.GetOrAdd(featureName, (name) => ReadFeatureDefinition(name));

            return Task.FromResult(definition);
        }

        /// <summary>
        /// Retrieves definitions for all features.
        /// </summary>
        /// <returns>An enumerator which provides asynchronous iteration over feature definitions.</returns>
        //
        // The async key word is necessary for creating IAsyncEnumerable.
        // The need to disable this warning occurs when implementaing async stream synchronously. 
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
#pragma warning restore CS1998
        {
            if (Interlocked.Exchange(ref _stale, 0) != 0)
            {
                _definitions.Clear();
            }

            //
            // Iterate over all features registered in the system at initial invocation time
            foreach (IConfigurationSection featureSection in GetFeatureDefinitionSections())
            {
                string featureName = GetFeatureName(featureSection);

                if (string.IsNullOrEmpty(featureName))
                {
                    continue;
                }

                //
                // Underlying IConfigurationSection data is dynamic so latest feature definitions are returned
                yield return  _definitions.GetOrAdd(featureName, (_) => ReadFeatureDefinition(featureSection));
            }
        }

        private FeatureDefinition ReadFeatureDefinition(string featureName)
        {
            IConfigurationSection configuration = GetFeatureDefinitionSections()
                .FirstOrDefault(section => string.Equals(GetFeatureName(section), featureName, StringComparison.OrdinalIgnoreCase));

            if (configuration == null)
            {
                return null;
            }

            return ReadFeatureDefinition(configuration);
        }

        private FeatureDefinition ReadFeatureDefinition(IConfigurationSection configurationSection)
        {
            if (_microsoftFeatureFlagSchemaEnabled)
            {
                return ParseMicrosoftFeatureDefinition(configurationSection);
            }

            return ParseFeatureDefinition(configurationSection);
        }

        private FeatureDefinition ParseFeatureDefinition(IConfigurationSection configurationSection)
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
            },
            myAllRequiredFilterFeature: {
                requirementType: "all"
                enabledFor: [ "myFeatureFilter1", "myFeatureFilter2" ],
            },

            */

            string featureName = GetFeatureName(configurationSection);

            var enabledFor = new List<FeatureFilterConfiguration>();

            RequirementType requirementType = RequirementType.Any;

            string val = configurationSection.Value; // configuration[$"{featureName}"];

            if (string.IsNullOrEmpty(val))
            {
                val = configurationSection[ConfigurationFields.FeatureFiltersSectionName];
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
                string rawRequirementType = configurationSection[ConfigurationFields.RequirementType];

                //
                // If requirement type is specified, parse it and set the requirementType variable
                if (!string.IsNullOrEmpty(rawRequirementType) && !Enum.TryParse(rawRequirementType, ignoreCase: true, out requirementType))
                {
                    throw new FeatureManagementException(
                        FeatureManagementError.InvalidConfigurationSetting,
                        $"Invalid value '{rawRequirementType}' for field '{ConfigurationFields.RequirementType}' of feature '{featureName}'.");
                }

                IEnumerable<IConfigurationSection> filterSections = configurationSection.GetSection(ConfigurationFields.FeatureFiltersSectionName).GetChildren();

                foreach (IConfigurationSection section in filterSections)
                {
                    //
                    // Arrays in json such as "myKey": [ "some", "values" ]
                    // Are accessed through the configuration system by using the array index as the property name, e.g. "myKey": { "0": "some", "1": "values" }
                    if (int.TryParse(section.Key, out int _) && !string.IsNullOrEmpty(section[ConfigurationFields.NameKeyword]))
                    {
                        enabledFor.Add(new FeatureFilterConfiguration()
                        {
                            Name = section[ConfigurationFields.NameKeyword],
                            Parameters = new ConfigurationWrapper(section.GetSection(ConfigurationFields.FeatureFilterConfigurationParameters))
                        });
                    }
                }
            }

            return new FeatureDefinition()
            {
                Name = featureName,
                EnabledFor = enabledFor,
                RequirementType = requirementType
            };
        }

        private FeatureDefinition ParseMicrosoftFeatureDefinition(IConfigurationSection configurationSection)
        {
            /*
            
            If Microsoft feature flag schema is enabled, we support

            FeatureFlags: [
              {
                id: "myFeature",
                enabled: true,
                conditions: {
                  client_filters: ["myFeatureFilter1", "myFeatureFilter2"],
                  requirement_type: "All",
                }
              },
              {
                id: "myAlwaysEnabledFeature",
                enabled: true,
                conditions: {
                  client_filters: [],
                }
              },
              {
                id: "myAlwaysDisabledFeature",
                enabled: false,
              }
            ]

            */

            string featureName = GetFeatureName(configurationSection);

            var enabledFor = new List<FeatureFilterConfiguration>();

            RequirementType requirementType = RequirementType.Any;

            IConfigurationSection conditions = configurationSection.GetSection(MicrosoftFeatureManagementFields.Conditions);

            string rawEnabled = configurationSection[MicrosoftFeatureManagementFields.Enabled];

            bool enabled = false;

            if (!string.IsNullOrEmpty(rawEnabled) && !bool.TryParse(rawEnabled, out enabled))
            {
                throw new FeatureManagementException(
                    FeatureManagementError.InvalidConfigurationSetting,
                    $"Invalid value '{rawEnabled}' for field '{MicrosoftFeatureManagementFields.Enabled}' of feature '{featureName}'.");
            }

            if (enabled)
            {
                string rawRequirementType = conditions[MicrosoftFeatureManagementFields.RequirementType];

                //
                // If requirement type is specified, parse it and set the requirementType variable
                if (!string.IsNullOrEmpty(rawRequirementType) && !Enum.TryParse(rawRequirementType, ignoreCase: true, out requirementType))
                {
                    throw new FeatureManagementException(
                        FeatureManagementError.InvalidConfigurationSetting,
                        $"Invalid value '{rawRequirementType}' for field '{MicrosoftFeatureManagementFields.RequirementType}' of feature '{featureName}'.");
                }

                IEnumerable<IConfigurationSection> filterSections = conditions.GetSection(MicrosoftFeatureManagementFields.ClientFilters).GetChildren();

                if (filterSections.Any())
                {
                    foreach (IConfigurationSection section in filterSections)
                    {
                        //
                        // Arrays in json such as "myKey": [ "some", "values" ]
                        // Are accessed through the configuration system by using the array index as the property name, e.g. "myKey": { "0": "some", "1": "values" }
                        if (int.TryParse(section.Key, out int _) && !string.IsNullOrEmpty(section[MicrosoftFeatureManagementFields.Name]))
                        {
                            enabledFor.Add(new FeatureFilterConfiguration()
                            {
                                Name = section[MicrosoftFeatureManagementFields.Name],
                                Parameters = new ConfigurationWrapper(section.GetSection(MicrosoftFeatureManagementFields.Parameters))
                            });
                        }
                    }
                }
                else
                {
                    enabledFor.Add(new FeatureFilterConfiguration
                    {
                        Name = "AlwaysOn"
                    });
                }
            }

            return new FeatureDefinition()
            {
                Name = featureName,
                EnabledFor = enabledFor,
                RequirementType = requirementType
            };
        }

        private string GetFeatureName(IConfigurationSection section)
        {
            if (_microsoftFeatureFlagSchemaEnabled)
            {
                return section[MicrosoftFeatureManagementFields.Id];
            }

            return section.Key;
        }

        private IEnumerable<IConfigurationSection> GetFeatureDefinitionSections()
        {
            if (!_configuration.GetChildren().Any())
            {
                Logger?.LogDebug($"Configuration is empty.");

                return Enumerable.Empty<IConfigurationSection>();
            }

            IConfiguration featureManagementConfigurationSection = _configuration
                    .GetChildren()
                    .FirstOrDefault(section =>
                        string.Equals(
                            section.Key,
                            _microsoftFeatureFlagSchemaEnabled ? 
                                MicrosoftFeatureManagementFields.FeatureManagementSectionName : 
                                ConfigurationFields.FeatureManagementSectionName,
                            StringComparison.OrdinalIgnoreCase));

            if (featureManagementConfigurationSection == null)
            {
                if (RootConfigurationFallbackEnabled)
                {
                    featureManagementConfigurationSection = _configuration;
                }
                else
                {
                    Logger?.LogDebug($"No configuration section named '{ConfigurationFields.FeatureManagementSectionName}' was found.");

                    return Enumerable.Empty<IConfigurationSection>();
                }
            }

            if (_microsoftFeatureFlagSchemaEnabled)
            {
                IConfigurationSection featureFlagsSection = featureManagementConfigurationSection.GetSection(MicrosoftFeatureManagementFields.FeatureFlagsSectionName);

                return featureFlagsSection.GetChildren();
            }

            return featureManagementConfigurationSection.GetChildren();
        }
    }
}
