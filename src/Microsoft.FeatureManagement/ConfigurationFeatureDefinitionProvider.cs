// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private long _initialized = 0;
        private bool _microsoftFeatureFlagSchemaEnabled;
        private readonly object _lock = new object();

        const string ParseValueErrorString = "Invalid setting '{0}' with value '{1}' for feature '{2}'.";

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

            EnsureInit();

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
            EnsureInit();

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

        private void EnsureInit()
        {
            if (_initialized == 0)
            {
                IConfiguration featureManagementConfigurationSection = _configuration
                    .GetChildren()
                    .FirstOrDefault(section =>
                        string.Equals(
                            section.Key,
                            ConfigurationFields.FeatureManagementSectionName,
                            StringComparison.OrdinalIgnoreCase));

                if (featureManagementConfigurationSection == null && RootConfigurationFallbackEnabled)
                {
                    featureManagementConfigurationSection = _configuration;
                }

                bool hasMicrosoftFeatureFlagSchema = featureManagementConfigurationSection != null && 
                    HasMicrosoftFeatureFlagSchema(featureManagementConfigurationSection);

                lock (_lock)
                {
                    if (Interlocked.Read(ref _initialized) == 0)
                    {
                        _microsoftFeatureFlagSchemaEnabled = hasMicrosoftFeatureFlagSchema;

                        Interlocked.Exchange(ref _initialized, 1);
                    }
                }
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

            FeatureStatus featureStatus = FeatureStatus.Conditional;

            Allocation allocation = null;

            List<VariantDefinition> variants = null;

            bool telemetryEnabled = false;

            Dictionary<string, string> telemetryMetadata = null;

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

                string rawFeatureStatus = configurationSection[ConfigurationFields.FeatureStatus];

                if (!string.IsNullOrEmpty(rawRequirementType))
                {
                    requirementType = ParseEnum<RequirementType>(featureName, rawRequirementType, ConfigurationFields.RequirementType);
                }

                if (!string.IsNullOrEmpty(rawFeatureStatus))
                {
                    featureStatus = ParseEnum<FeatureStatus>(featureName, rawFeatureStatus, ConfigurationFields.FeatureStatus);
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

                IConfigurationSection allocationSection = configurationSection.GetSection(ConfigurationFields.AllocationSectionName);

                if (allocationSection.Exists())
                {
                    allocation = new Allocation()
                    {
                        DefaultWhenDisabled = allocationSection[ConfigurationFields.AllocationDefaultWhenDisabled],
                        DefaultWhenEnabled = allocationSection[ConfigurationFields.AllocationDefaultWhenEnabled],
                        User = allocationSection.GetSection(ConfigurationFields.UserAllocationSectionName).GetChildren().Select(userAllocation =>
                        {
                            return new UserAllocation()
                            {
                                Variant = userAllocation[ConfigurationFields.AllocationVariantKeyword],
                                Users = userAllocation.GetSection(ConfigurationFields.UserAllocationUsers).Get<IEnumerable<string>>()
                            };
                        }),
                        Group = allocationSection.GetSection(ConfigurationFields.GroupAllocationSectionName).GetChildren().Select(groupAllocation =>
                        {
                            return new GroupAllocation()
                            {
                                Variant = groupAllocation[ConfigurationFields.AllocationVariantKeyword],
                                Groups = groupAllocation.GetSection(ConfigurationFields.GroupAllocationGroups).Get<IEnumerable<string>>()
                            };
                        }),
                        Percentile = allocationSection.GetSection(ConfigurationFields.PercentileAllocationSectionName).GetChildren().Select(percentileAllocation =>
                        {
                            double from = 0;

                            double to = 0;

                            string rawFrom = percentileAllocation[ConfigurationFields.PercentileAllocationFrom];

                            string rawTo = percentileAllocation[ConfigurationFields.PercentileAllocationTo];

                            if (!string.IsNullOrEmpty(rawFrom))
                            {
                                from = ParseDouble(configurationSection.Key, rawFrom, ConfigurationFields.PercentileAllocationFrom);
                            }

                            if (!string.IsNullOrEmpty(rawTo))
                            {
                                to = ParseDouble(configurationSection.Key, rawTo, ConfigurationFields.PercentileAllocationTo);
                            }

                            return new PercentileAllocation()
                            {
                                Variant = percentileAllocation[ConfigurationFields.AllocationVariantKeyword],
                                From = from,
                                To = to
                            };
                        }),
                        Seed = allocationSection[ConfigurationFields.AllocationSeed]
                    };
                }

                IEnumerable<IConfigurationSection> variantsSections = configurationSection.GetSection(ConfigurationFields.VariantsSectionName).GetChildren();
                variants = new List<VariantDefinition>();

                foreach (IConfigurationSection section in variantsSections)
                {
                    if (int.TryParse(section.Key, out int _) && !string.IsNullOrEmpty(section[ConfigurationFields.NameKeyword]))
                    {
                        StatusOverride statusOverride = StatusOverride.None;

                        string rawStatusOverride = section[ConfigurationFields.VariantDefinitionStatusOverride];

                        if (!string.IsNullOrEmpty(rawStatusOverride))
                        {
                            statusOverride = ParseEnum<StatusOverride>(configurationSection.Key, rawStatusOverride, ConfigurationFields.VariantDefinitionStatusOverride);
                        }

                        VariantDefinition variant = new VariantDefinition()
                        {
                            Name = section[ConfigurationFields.NameKeyword],
                            ConfigurationValue = section.GetSection(ConfigurationFields.VariantDefinitionConfigurationValue),
                            ConfigurationReference = section[ConfigurationFields.VariantDefinitionConfigurationReference],
                            StatusOverride = statusOverride
                        };

                        variants.Add(variant);
                    }
                }

                IConfigurationSection telemetrySection = configurationSection.GetSection(ConfigurationFields.Telemetry);

                if (telemetrySection.Exists())
                {
                    string rawTelemetryEnabled = telemetrySection[ConfigurationFields.Enabled];

                    if (!string.IsNullOrEmpty(rawTelemetryEnabled))
                    {
                        telemetryEnabled = ParseBool(featureName, rawTelemetryEnabled, ConfigurationFields.Enabled);
                    }

                    IConfigurationSection telemetryMetadataSection = telemetrySection.GetSection(ConfigurationFields.Metadata);

                    if (telemetryMetadataSection.Exists())
                    {
                        telemetryMetadata = new Dictionary<string, string>();

                        telemetryMetadata = telemetryMetadataSection.GetChildren().ToDictionary(x => x.Key, x => x.Value);
                    }
                }
            }

            return new FeatureDefinition()
            {
                Name = featureName,
                EnabledFor = enabledFor,
                RequirementType = requirementType,
                Status = featureStatus,
                Allocation = allocation,
                Variants = variants,
                Telemetry = new TelemetryConfiguration
                {
                    Enabled = telemetryEnabled,
                    Metadata = telemetryMetadata
                }
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

            bool enabled = false;

            IConfigurationSection conditions = configurationSection.GetSection(MicrosoftFeatureFlagFields.Conditions);

            string rawRequirementType = conditions[MicrosoftFeatureFlagFields.RequirementType];

            string rawEnabled = configurationSection[MicrosoftFeatureFlagFields.Enabled];

            if (!string.IsNullOrEmpty(rawEnabled))
            {
                enabled = ParseBool(featureName, rawEnabled, MicrosoftFeatureFlagFields.Enabled);
            }

            if (!string.IsNullOrEmpty(rawRequirementType))
            {
                requirementType = ParseEnum<RequirementType>(featureName, rawRequirementType, MicrosoftFeatureFlagFields.RequirementType);
            }

            if (enabled)
            {
                IEnumerable<IConfigurationSection> filterSections = conditions.GetSection(MicrosoftFeatureFlagFields.ClientFilters).GetChildren();

                if (filterSections.Any())
                {
                    foreach (IConfigurationSection section in filterSections)
                    {
                        //
                        // Arrays in json such as "myKey": [ "some", "values" ]
                        // Are accessed through the configuration system by using the array index as the property name, e.g. "myKey": { "0": "some", "1": "values" }
                        if (int.TryParse(section.Key, out int _) && !string.IsNullOrEmpty(section[MicrosoftFeatureFlagFields.Name]))
                        {
                            enabledFor.Add(new FeatureFilterConfiguration()
                            {
                                Name = section[MicrosoftFeatureFlagFields.Name],
                                Parameters = new ConfigurationWrapper(section.GetSection(MicrosoftFeatureFlagFields.Parameters))
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
                return section[MicrosoftFeatureFlagFields.Id];
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
                IConfigurationSection featureFlagsSection = featureManagementConfigurationSection.GetSection(MicrosoftFeatureFlagFields.FeatureFlagsSectionName);

                return featureFlagsSection.GetChildren();
            }

            return featureManagementConfigurationSection.GetChildren();
        }

        private static bool HasMicrosoftFeatureFlagSchema(IConfiguration featureManagementConfiguration)
        {
            IConfigurationSection featureFlagsConfigurationSection = featureManagementConfiguration
              .GetChildren()
              .FirstOrDefault(section =>
                  string.Equals(
                      section.Key,
                      MicrosoftFeatureFlagFields.FeatureFlagsSectionName,
                      StringComparison.OrdinalIgnoreCase));

            if (featureFlagsConfigurationSection != null)
            {
                if (!string.IsNullOrEmpty(featureFlagsConfigurationSection.Value))
                {
                    return false;
                }

                IEnumerable<IConfigurationSection> featureFlagsChildren = featureFlagsConfigurationSection.GetChildren();

                return featureFlagsChildren.Any() && featureFlagsChildren.All(section => int.TryParse(section.Key, out int _));
            }

            return false;
        }

        private T ParseEnum<T>(string feature, string rawValue, string fieldKeyword)
            where T: struct, Enum
        {
            Debug.Assert(!string.IsNullOrEmpty(rawValue));

            if (!Enum.TryParse(rawValue, ignoreCase: true, out T value))
            {
                throw new FeatureManagementException(
                    FeatureManagementError.InvalidConfigurationSetting,
                    string.Format(ParseValueErrorString, fieldKeyword, rawValue, feature));
            }

            return value;
        }

        private double ParseDouble(string feature, string rawValue, string fieldKeyword)
        {
            Debug.Assert(!string.IsNullOrEmpty(rawValue));

            if (!double.TryParse(rawValue, out double value))
            {
                throw new FeatureManagementException(
                    FeatureManagementError.InvalidConfigurationSetting,
                    string.Format(ParseValueErrorString, fieldKeyword, rawValue, feature));
            }

            return value;
        }

        private bool ParseBool(string feature, string rawValue, string fieldKeyword)
        {
            Debug.Assert(!string.IsNullOrEmpty(rawValue));

            if (!bool.TryParse(rawValue, out bool value))
            {
                throw new FeatureManagementException(
                    FeatureManagementError.InvalidConfigurationSetting,
                    string.Format(ParseValueErrorString, fieldKeyword, rawValue, feature));
            }

            return value;
        }
    }
}
