﻿// Copyright (c) Microsoft Corporation.
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
        private readonly bool _microsoftFeatureManagementSchemaEnabled;

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

            IConfiguration MicrosoftFeatureManagementConfigurationSection = _configuration
                .GetChildren()
                .FirstOrDefault(section =>
                    string.Equals(
                        section.Key,
                        MicrosoftFeatureManagementFields.FeatureManagementSectionName,
                        StringComparison.OrdinalIgnoreCase));

            if (MicrosoftFeatureManagementConfigurationSection != null)
            {
                _microsoftFeatureManagementSchemaEnabled = true;
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
            if (_microsoftFeatureManagementSchemaEnabled)
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
              enabledFor: [{name: "myFeatureFilter1"}, {name: "myFeatureFilter2"}]
            },
            myDisabledFeature: {
              enabledFor: [  ]
            },
            myAlwaysEnabledFeature: true,
            myAlwaysDisabledFeature: false // removing this line would be the same as setting it to false
            myAlwaysEnabledFeature2: {
              enabledFor: true
            },
            myAlwaysDisabledFeature2: {
              enabledFor: false
            },
            myAllRequiredFilterFeature: {
                requirementType: "All",
                enabledFor: [{name: "myFeatureFilter1"}, {name: "myFeatureFilter2"}]
            }

            */

            string featureName = GetFeatureName(configurationSection);

            var enabledFor = new List<FeatureFilterConfiguration>();

            RequirementType requirementType = RequirementType.Any;

            FeatureStatus featureStatus = FeatureStatus.Conditional;

            Allocation allocation = null;

            var variants = new List<VariantDefinition>();

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
                                from = ParseDouble(featureName, rawFrom, ConfigurationFields.PercentileAllocationFrom);
                            }

                            if (!string.IsNullOrEmpty(rawTo))
                            {
                                to = ParseDouble(featureName, rawTo, ConfigurationFields.PercentileAllocationTo);
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

                        var variant = new VariantDefinition()
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

            FeatureStatus featureStatus = FeatureStatus.Disabled;

            Allocation allocation = null;

            var variants = new List<VariantDefinition>();

            bool telemetryEnabled = false;

            Dictionary<string, string> telemetryMetadata = null;

            IConfigurationSection conditionsSection = configurationSection.GetSection(MicrosoftFeatureManagementFields.Conditions);

            string rawEnabled = configurationSection[MicrosoftFeatureManagementFields.Enabled];

            if (!string.IsNullOrEmpty(rawEnabled))
            {
                enabled = ParseBool(featureName, rawEnabled, MicrosoftFeatureManagementFields.Enabled);
            }

            if (enabled)
            {
                string rawRequirementType = conditionsSection[MicrosoftFeatureManagementFields.RequirementType];

                //
                // If requirement type is specified, parse it and set the requirementType variable
                if (!string.IsNullOrEmpty(rawRequirementType) && !Enum.TryParse(rawRequirementType, ignoreCase: true, out requirementType))
                {
                    throw new FeatureManagementException(
                        FeatureManagementError.InvalidConfigurationSetting,
                        $"Invalid value '{rawRequirementType}' for field '{MicrosoftFeatureManagementFields.RequirementType}' of feature '{featureName}'.");
                }

                featureStatus = FeatureStatus.Conditional;

                IEnumerable<IConfigurationSection> filterSections = conditionsSection.GetSection(MicrosoftFeatureManagementFields.ClientFilters).GetChildren();

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

            IConfigurationSection allocationSection = configurationSection.GetSection(MicrosoftFeatureManagementFields.AllocationSectionName);

            if (allocationSection.Exists())
            {
                allocation = new Allocation()
                {
                    DefaultWhenDisabled = allocationSection[MicrosoftFeatureManagementFields.AllocationDefaultWhenDisabled],
                    DefaultWhenEnabled = allocationSection[MicrosoftFeatureManagementFields.AllocationDefaultWhenEnabled],
                    User = allocationSection.GetSection(MicrosoftFeatureManagementFields.UserAllocationSectionName).GetChildren().Select(userAllocation =>
                    {
                        return new UserAllocation()
                        {
                            Variant = userAllocation[MicrosoftFeatureManagementFields.AllocationVariantKeyword],
                            Users = userAllocation.GetSection(MicrosoftFeatureManagementFields.UserAllocationUsers).Get<IEnumerable<string>>()
                        };
                    }),
                    Group = allocationSection.GetSection(MicrosoftFeatureManagementFields.GroupAllocationSectionName).GetChildren().Select(groupAllocation =>
                    {
                        return new GroupAllocation()
                        {
                            Variant = groupAllocation[MicrosoftFeatureManagementFields.AllocationVariantKeyword],
                            Groups = groupAllocation.GetSection(MicrosoftFeatureManagementFields.GroupAllocationGroups).Get<IEnumerable<string>>()
                        };
                    }),
                    Percentile = allocationSection.GetSection(MicrosoftFeatureManagementFields.PercentileAllocationSectionName).GetChildren().Select(percentileAllocation =>
                    {
                        double from = 0;

                        double to = 0;

                        string rawFrom = percentileAllocation[MicrosoftFeatureManagementFields.PercentileAllocationFrom];

                        string rawTo = percentileAllocation[MicrosoftFeatureManagementFields.PercentileAllocationTo];

                        if (!string.IsNullOrEmpty(rawFrom))
                        {
                            from = ParseDouble(featureName, rawFrom, MicrosoftFeatureManagementFields.PercentileAllocationFrom);
                        }

                        if (!string.IsNullOrEmpty(rawTo))
                        {
                            to = ParseDouble(featureName, rawTo, MicrosoftFeatureManagementFields.PercentileAllocationTo);
                        }

                        return new PercentileAllocation()
                        {
                            Variant = percentileAllocation[MicrosoftFeatureManagementFields.AllocationVariantKeyword],
                            From = from,
                            To = to
                        };
                    }),
                    Seed = allocationSection[MicrosoftFeatureManagementFields.AllocationSeed]
                };
            }

            IEnumerable<IConfigurationSection> variantsSections = configurationSection.GetSection(MicrosoftFeatureManagementFields.VariantsSectionName).GetChildren();

            foreach (IConfigurationSection section in variantsSections)
            {
                if (int.TryParse(section.Key, out int _) && !string.IsNullOrEmpty(section[MicrosoftFeatureManagementFields.Name]))
                {
                    StatusOverride statusOverride = StatusOverride.None;

                    string rawStatusOverride = section[MicrosoftFeatureManagementFields.VariantDefinitionStatusOverride];

                    if (!string.IsNullOrEmpty(rawStatusOverride))
                    {
                        statusOverride = ParseEnum<StatusOverride>(configurationSection.Key, rawStatusOverride, MicrosoftFeatureManagementFields.VariantDefinitionStatusOverride);
                    }

                    var variant = new VariantDefinition()
                    {
                        Name = section[MicrosoftFeatureManagementFields.Name],
                        ConfigurationValue = section.GetSection(MicrosoftFeatureManagementFields.VariantDefinitionConfigurationValue),
                        ConfigurationReference = section[MicrosoftFeatureManagementFields.VariantDefinitionConfigurationReference],
                        StatusOverride = statusOverride
                    };

                    variants.Add(variant);
                }
            }

            IConfigurationSection telemetrySection = configurationSection.GetSection(MicrosoftFeatureManagementFields.Telemetry);

            if (telemetrySection.Exists())
            {
                string rawTelemetryEnabled = telemetrySection[MicrosoftFeatureManagementFields.Enabled];

                if (!string.IsNullOrEmpty(rawTelemetryEnabled))
                {
                    telemetryEnabled = ParseBool(featureName, rawTelemetryEnabled, MicrosoftFeatureManagementFields.Enabled);
                }

                IConfigurationSection telemetryMetadataSection = telemetrySection.GetSection(MicrosoftFeatureManagementFields.Metadata);

                if (telemetryMetadataSection.Exists())
                {
                    telemetryMetadata = new Dictionary<string, string>();

                    telemetryMetadata = telemetryMetadataSection.GetChildren().ToDictionary(x => x.Key, x => x.Value);
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

        private string GetFeatureName(IConfigurationSection section)
        {
            if (_microsoftFeatureManagementSchemaEnabled)
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
                            _microsoftFeatureManagementSchemaEnabled ? 
                                MicrosoftFeatureManagementFields.FeatureManagementSectionName : 
                                ConfigurationFields.FeatureManagementSectionName,
                            StringComparison.OrdinalIgnoreCase));

            if (featureManagementConfigurationSection == null)
            {
                if (RootConfigurationFallbackEnabled && !_microsoftFeatureManagementSchemaEnabled)
                {
                    featureManagementConfigurationSection = _configuration;
                }
                else
                {
                    Logger?.LogDebug($"No feature management configuration section was found.");

                    return Enumerable.Empty<IConfigurationSection>();
                }
            }

            if (_microsoftFeatureManagementSchemaEnabled)
            {
                IConfigurationSection featureFlagsSection = featureManagementConfigurationSection.GetSection(MicrosoftFeatureManagementFields.FeatureFlagsSectionName);

                return featureFlagsSection.GetChildren();
            }

            return featureManagementConfigurationSection.GetChildren();
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
