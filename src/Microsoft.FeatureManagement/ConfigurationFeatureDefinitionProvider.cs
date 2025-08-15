// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly ConfigurationFeatureDefinitionProviderOptions _options;
        private IEnumerable<IConfigurationSection> _dotnetFeatureDefinitionSections;
        private IEnumerable<IConfigurationSection> _microsoftFeatureDefinitionSections;
        private readonly ConcurrentDictionary<string, Task<FeatureDefinition>> _definitions;
        private IDisposable _changeSubscription;
        private int _stale = 0;
        private int _initialized = 0;
        private readonly Func<string, Task<FeatureDefinition>> _getFeatureDefinitionFunc;

        const string ParseValueErrorString = "Invalid setting '{0}' with value '{1}' for feature '{2}'.";

        /// <summary>
        /// Creates a configuration feature definition provider.
        /// </summary>
        /// <param name="configuration">The configuration of feature definitions.</param>
        /// <param name="options">The options for the configuration feature definition provider.</param>
        public ConfigurationFeatureDefinitionProvider(
            IConfiguration configuration,
            ConfigurationFeatureDefinitionProviderOptions options = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _options = options ?? new ConfigurationFeatureDefinitionProviderOptions();
            _definitions = new ConcurrentDictionary<string, Task<FeatureDefinition>>();

            _changeSubscription = ChangeToken.OnChange(
                () => _configuration.GetReloadToken(),
                () => _stale = 1);

            _getFeatureDefinitionFunc = (featureName) =>
            {
                return Task.FromResult(GetMicrosoftSchemaFeatureDefinition(featureName) ?? GetDotnetSchemaFeatureDefinition(featureName));
            };
        }

        /// <summary>
        /// The option that controls the behavior when "FeatureManagement" section in the configuration is missing.
        /// </summary>
        public bool RootConfigurationFallbackEnabled { get; set; }

        /// <summary>
        /// The logger for the configuration feature definition provider.
        /// </summary>
        public ILogger Logger { get; set; }

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
                _dotnetFeatureDefinitionSections = GetDotnetFeatureDefinitionSections();

                _microsoftFeatureDefinitionSections = GetMicrosoftFeatureDefinitionSections();

                _definitions.Clear();
            }

            return _definitions.GetOrAdd(featureName, _getFeatureDefinitionFunc);
        }

        /// <summary>
        /// Retrieves definitions for all features.
        /// </summary>
        /// <returns>An enumerator which provides asynchronous iteration over feature definitions.</returns>
        //
        // The async key word is necessary for creating IAsyncEnumerable.
        // The need to disable this warning occurs when implementing async stream synchronously. 
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
#pragma warning restore CS1998
        {
            EnsureInit();

            if (Interlocked.Exchange(ref _stale, 0) != 0)
            {
                _dotnetFeatureDefinitionSections = GetDotnetFeatureDefinitionSections();

                _microsoftFeatureDefinitionSections = GetMicrosoftFeatureDefinitionSections();

                _definitions.Clear();
            }

            foreach (IConfigurationSection featureSection in _microsoftFeatureDefinitionSections)
            {
                string featureName = featureSection[MicrosoftFeatureManagementFields.Id];

                if (string.IsNullOrEmpty(featureName))
                {
                    continue;
                }

                //
                // Underlying IConfigurationSection data is dynamic so latest feature definitions are returned
                FeatureDefinition definition = _definitions.GetOrAdd(featureName, _getFeatureDefinitionFunc).Result;

                if (definition != null)
                {
                    yield return definition;
                }
            }

            foreach (IConfigurationSection featureSection in _dotnetFeatureDefinitionSections)
            {
                string featureName = featureSection.Key;

                if (string.IsNullOrEmpty(featureName))
                {
                    continue;
                }

                //
                // Underlying IConfigurationSection data is dynamic so latest feature definitions are returned
                FeatureDefinition definition = _definitions.GetOrAdd(featureName, _getFeatureDefinitionFunc).Result;

                if (definition != null)
                {
                    yield return definition;
                }
            }
        }

        private void EnsureInit()
        {
            if (_initialized == 0)
            {
                _dotnetFeatureDefinitionSections = GetDotnetFeatureDefinitionSections();

                _microsoftFeatureDefinitionSections = GetMicrosoftFeatureDefinitionSections();

                _initialized = 1;
            }
        }

        private FeatureDefinition GetDotnetSchemaFeatureDefinition(string featureName)
        {
            IConfigurationSection dotnetFeatureDefinitionConfiguration = _dotnetFeatureDefinitionSections
                .FirstOrDefault(section =>
                    string.Equals(section.Key, featureName, StringComparison.OrdinalIgnoreCase));

            if (dotnetFeatureDefinitionConfiguration == null)
            {
                return null;
            }

            return ParseDotnetSchemaFeatureDefinition(dotnetFeatureDefinitionConfiguration);
        }

        private FeatureDefinition GetMicrosoftSchemaFeatureDefinition(string featureName)
        {
            IConfigurationSection microsoftFeatureDefinitionConfiguration = _microsoftFeatureDefinitionSections
                .LastOrDefault(section =>
                    string.Equals(section[MicrosoftFeatureManagementFields.Id], featureName, StringComparison.OrdinalIgnoreCase));

            if (microsoftFeatureDefinitionConfiguration == null)
            {
                return null;
            }

            return ParseMicrosoftSchemaFeatureDefinition(microsoftFeatureDefinitionConfiguration);
        }

        private IEnumerable<IConfigurationSection> GetDotnetFeatureDefinitionSections()
        {
            IConfigurationSection featureManagementConfigurationSection = _configuration.GetSection(DotnetFeatureManagementFields.FeatureManagementSectionName);

            if (featureManagementConfigurationSection.Exists())
            {
                return featureManagementConfigurationSection.GetChildren();
            }

            //
            // Root configuration fallback only applies to .NET schema.
            // If Microsoft schema can be found, root configuration fallback will not be effective.
            if (RootConfigurationFallbackEnabled &&
                !_configuration.GetChildren()
                    .Any(section =>
                        string.Equals(section.Key, MicrosoftFeatureManagementFields.FeatureManagementSectionName, StringComparison.OrdinalIgnoreCase)))
            {
                return _configuration.GetChildren();
            }

            return Enumerable.Empty<IConfigurationSection>();
        }

        private IEnumerable<IConfigurationSection> GetMicrosoftFeatureDefinitionSections()
        {
            if (_options.DisableCustomConfigurationMerging)
            {
                return _configuration.GetSection(MicrosoftFeatureManagementFields.FeatureManagementSectionName)
                    .GetSection(MicrosoftFeatureManagementFields.FeatureFlagsSectionName)
                    .GetChildren();
            }

            var featureDefinitionSections = new List<IConfigurationSection>();

            FindFeatureFlags(_configuration, featureDefinitionSections);

            return featureDefinitionSections;
        }

        private void FindFeatureFlags(IConfiguration configuration, List<IConfigurationSection> featureDefinitionSections)
        {
            if (!(configuration is IConfigurationRoot configurationRoot) ||
                configurationRoot.Providers.Any(provider =>
                    !(provider is ConfigurationProvider) && !(provider is ChainedConfigurationProvider)))
            {
                IConfigurationSection featureFlagsSection = configuration
                    .GetSection(MicrosoftFeatureManagementFields.FeatureManagementSectionName)
                    .GetSection(MicrosoftFeatureManagementFields.FeatureFlagsSectionName);

                if (featureFlagsSection.Exists())
                {
                    featureDefinitionSections.AddRange(featureFlagsSection.GetChildren());
                }

                return;
            }

            foreach (IConfigurationProvider provider in configurationRoot.Providers)
            {
                if (provider is ConfigurationProvider configurationProvider)
                {
                    //
                    // Cannot use the original provider directly as its reload token is subscribed
                    var onDemandConfigurationProvider = new OnDemandConfigurationProvider(configurationProvider);

                    var onDemandConfigurationRoot = new ConfigurationRoot(new[] { onDemandConfigurationProvider });

                    IConfigurationSection featureFlagsSection = onDemandConfigurationRoot
                        .GetSection(MicrosoftFeatureManagementFields.FeatureManagementSectionName)
                        .GetSection(MicrosoftFeatureManagementFields.FeatureFlagsSectionName);

                    if (featureFlagsSection.Exists())
                    {
                        featureDefinitionSections.AddRange(featureFlagsSection.GetChildren());
                    }
                }
                else if (provider is ChainedConfigurationProvider chainedProvider)
                {
                    FindFeatureFlags(chainedProvider.Configuration, featureDefinitionSections);
                }
            }
        }

        private FeatureDefinition ParseDotnetSchemaFeatureDefinition(IConfigurationSection configurationSection)
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

            string featureName = configurationSection.Key;

            var enabledFor = new List<FeatureFilterConfiguration>();

            RequirementType requirementType = RequirementType.Any;

            FeatureStatus featureStatus = FeatureStatus.Conditional;

            string val = configurationSection.Value; // configuration[$"{featureName}"];

            if (string.IsNullOrEmpty(val))
            {
                val = configurationSection[DotnetFeatureManagementFields.FeatureFiltersSectionName];
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
                string rawRequirementType = configurationSection[DotnetFeatureManagementFields.RequirementType];

                if (!string.IsNullOrEmpty(rawRequirementType))
                {
                    requirementType = ParseEnum<RequirementType>(featureName, rawRequirementType, DotnetFeatureManagementFields.RequirementType);
                }

                IEnumerable<IConfigurationSection> filterSections = configurationSection.GetSection(DotnetFeatureManagementFields.FeatureFiltersSectionName).GetChildren();

                foreach (IConfigurationSection section in filterSections)
                {
                    //
                    // Arrays in json such as "myKey": [ "some", "values" ]
                    // Are accessed through the configuration system by using the array index as the property name, e.g. "myKey": { "0": "some", "1": "values" }
                    if (int.TryParse(section.Key, out int _) && !string.IsNullOrEmpty(section[DotnetFeatureManagementFields.NameKeyword]))
                    {
                        enabledFor.Add(new FeatureFilterConfiguration()
                        {
                            Name = section[DotnetFeatureManagementFields.NameKeyword],
                            Parameters = new ConfigurationWrapper(section.GetSection(DotnetFeatureManagementFields.FeatureFilterConfigurationParameters))
                        });
                    }
                }
            }

            return new FeatureDefinition()
            {
                Name = featureName,
                EnabledFor = enabledFor,
                RequirementType = requirementType,
                Status = featureStatus
            };
        }

        private FeatureDefinition ParseMicrosoftSchemaFeatureDefinition(IConfigurationSection configurationSection)
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

            string featureName = configurationSection[MicrosoftFeatureManagementFields.Id];

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

                if (!string.IsNullOrEmpty(rawRequirementType))
                {
                    requirementType = ParseEnum<RequirementType>(featureName, rawRequirementType, MicrosoftFeatureManagementFields.RequirementType);
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

        private static T ParseEnum<T>(string feature, string rawValue, string fieldKeyword)
            where T : struct, Enum
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

        private static double ParseDouble(string feature, string rawValue, string fieldKeyword)
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

        private static bool ParseBool(string feature, string rawValue, string fieldKeyword)
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
