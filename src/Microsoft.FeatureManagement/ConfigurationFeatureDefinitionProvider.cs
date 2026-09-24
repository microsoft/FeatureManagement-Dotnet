// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A feature definition provider that pulls feature definitions from the .NET Core <see cref="IConfiguration"/> system.
    /// </summary>
    public sealed class ConfigurationFeatureDefinitionProvider : IFeatureDefinitionProvider, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ConfigurationFeatureDefinitionProviderOptions _options;
        private IEnumerable<FeatureDefinitionSectionsBySource> _featureDefinitionSources;
        private readonly ConcurrentDictionary<string, Task<FeatureDefinition>> _definitions;
        private IDisposable _changeSubscription;
        private int _stale = 0;
        private int _initialized = 0;
        private readonly Func<string, Task<FeatureDefinition>> _getFeatureDefinitionFunc;

        const string ParseValueErrorString = "Invalid setting '{0}' with value '{1}' for feature '{2}'.";

        private sealed class FeatureDefinitionSectionsBySource
        {
            public FeatureDefinitionSectionsBySource(
                IEnumerable<IConfigurationSection> dotnetSections,
                IEnumerable<IConfigurationSection> microsoftSections)
            {
                DotnetSections = dotnetSections;
                MicrosoftSections = microsoftSections;
            }

            public IEnumerable<IConfigurationSection> DotnetSections { get; }

            public IEnumerable<IConfigurationSection> MicrosoftSections { get; }
        }

        /// <summary>
        /// Creates a configuration feature definition provider.
        /// </summary>
        /// <param name="configuration">The configuration of feature definitions.</param>
        public ConfigurationFeatureDefinitionProvider(IConfiguration configuration) : this(configuration, null)
        {
        }

        /// <summary>
        /// Creates a configuration feature definition provider.
        /// </summary>
        /// <param name="configuration">The configuration of feature definitions.</param>
        /// <param name="options">The options for the configuration feature definition provider.</param>
        public ConfigurationFeatureDefinitionProvider(
            IConfiguration configuration,
            ConfigurationFeatureDefinitionProviderOptions options)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _options = options ?? new ConfigurationFeatureDefinitionProviderOptions();
            _definitions = new ConcurrentDictionary<string, Task<FeatureDefinition>>();

            _changeSubscription = ChangeToken.OnChange(
                () => _configuration.GetReloadToken(),
                () => _stale = 1);

            _getFeatureDefinitionFunc = (featureName) =>
            {
                return Task.FromResult(GetFeatureDefinition(featureName));
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
                LoadFeatureDefinitionSections();

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
                LoadFeatureDefinitionSections();

                _definitions.Clear();
            }

            HashSet<string> processedFeatureNames = _options.CustomConfigurationMergingEnabled
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : null;

            foreach (FeatureDefinitionSectionsBySource source in _featureDefinitionSources)
            {
                IEnumerable<string> featureNames = source.MicrosoftSections
                    .Select(section => section[MicrosoftFeatureManagementFields.Id])
                    .Concat(source.DotnetSections.Select(section => section.Key));

                foreach (string featureName in featureNames)
                {
                    if (string.IsNullOrEmpty(featureName) ||
                        (processedFeatureNames != null && !processedFeatureNames.Add(featureName)))
                    {
                        continue;
                    }

                    FeatureDefinition definition = _definitions.GetOrAdd(featureName, _getFeatureDefinitionFunc).Result;

                    if (definition != null)
                    {
                        yield return definition;
                    }
                }
            }
        }

        private void EnsureInit()
        {
            if (_initialized == 0)
            {
                LoadFeatureDefinitionSections();

                _initialized = 1;
            }
        }

        private void LoadFeatureDefinitionSections()
        {
            //
            // Determine root fallback from the full configuration, not from individual providers.
            bool useRootConfiguration = RootConfigurationFallbackEnabled &&
                !_configuration.GetSection(DotnetFeatureManagementFields.FeatureManagementSectionName).Exists() &&
                !_configuration.GetChildren().Any(section =>
                    string.Equals(section.Key, MicrosoftFeatureManagementFields.FeatureManagementSectionName, StringComparison.OrdinalIgnoreCase));

            if (!_options.CustomConfigurationMergingEnabled)
            {
                _featureDefinitionSources = new[] { GetFeatureDefinitionSections(_configuration, useRootConfiguration) };
                return;
            }

            var featureDefinitionSources = new List<FeatureDefinitionSectionsBySource>();

            FindFeatureDefinitionSources(_configuration, useRootConfiguration, featureDefinitionSources);

            _featureDefinitionSources = featureDefinitionSources;
        }

        private FeatureDefinition GetFeatureDefinition(string featureName)
        {
            foreach (FeatureDefinitionSectionsBySource source in _featureDefinitionSources)
            {
                FeatureDefinition definition = GetMicrosoftSchemaFeatureDefinition(featureName, source.MicrosoftSections) ??
                    GetDotnetSchemaFeatureDefinition(featureName, source.DotnetSections);

                if (definition != null)
                {
                    return definition;
                }
            }

            return null;
        }

        private FeatureDefinition GetDotnetSchemaFeatureDefinition(string featureName, IEnumerable<IConfigurationSection> sections)
        {
            IConfigurationSection dotnetFeatureDefinitionConfiguration = sections
                .FirstOrDefault(section =>
                    string.Equals(section.Key, featureName, StringComparison.OrdinalIgnoreCase));

            if (dotnetFeatureDefinitionConfiguration == null)
            {
                return null;
            }

            return ParseDotnetSchemaFeatureDefinition(dotnetFeatureDefinitionConfiguration);
        }

        private FeatureDefinition GetMicrosoftSchemaFeatureDefinition(string featureName, IEnumerable<IConfigurationSection> sections)
        {
            IConfigurationSection microsoftFeatureDefinitionConfiguration = sections
                .LastOrDefault(section =>
                    string.Equals(section[MicrosoftFeatureManagementFields.Id], featureName, StringComparison.OrdinalIgnoreCase));

            if (microsoftFeatureDefinitionConfiguration == null)
            {
                return null;
            }

            return ParseMicrosoftSchemaFeatureDefinition(microsoftFeatureDefinitionConfiguration);
        }

        private FeatureDefinitionSectionsBySource GetFeatureDefinitionSections(IConfiguration configuration, bool useRootConfiguration)
        {
            return new FeatureDefinitionSectionsBySource(
                useRootConfiguration
                    ? configuration.GetChildren()
                    : configuration.GetSection(DotnetFeatureManagementFields.FeatureManagementSectionName).GetChildren(),
                configuration.GetSection(MicrosoftFeatureManagementFields.FeatureManagementSectionName)
                    .GetSection(MicrosoftFeatureManagementFields.FeatureFlagsSectionName)
                    .GetChildren());
        }

        private void FindFeatureDefinitionSources(
            IConfiguration configuration,
            bool useRootConfiguration,
            List<FeatureDefinitionSectionsBySource> featureDefinitionSources)
        {
            if (!(configuration is IConfigurationRoot configurationRoot) ||
                configurationRoot.Providers.Any(provider =>
                    !(provider is ConfigurationProvider) && !(provider is ChainedConfigurationProvider)))
            {
                featureDefinitionSources.Add(GetFeatureDefinitionSections(configuration, useRootConfiguration));
                return;
            }

            //
            // Keep sources in highest-to-lowest precedence order, including chained providers.
            foreach (IConfigurationProvider provider in configurationRoot.Providers.Reverse())
            {
                if (provider is ConfigurationProvider configurationProvider)
                {
                    //
                    // Cannot use the original provider directly as its reload token is subscribed
                    var onDemandConfigurationProvider = new OnDemandConfigurationProvider(configurationProvider);

                    var onDemandConfigurationRoot = new ConfigurationRoot(new[] { onDemandConfigurationProvider });

                    featureDefinitionSources.Add(GetFeatureDefinitionSections(onDemandConfigurationRoot, useRootConfiguration));
                }
                else if (provider is ChainedConfigurationProvider chainedProvider)
                {
                    FindFeatureDefinitionSources(chainedProvider.Configuration, useRootConfiguration, featureDefinitionSources);
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

                    var configurationValue = section.GetSection(
                        MicrosoftFeatureManagementFields.VariantDefinitionConfigurationValue);

                    var variant = new VariantDefinition()
                    {
                        Name = section[MicrosoftFeatureManagementFields.Name],
                        ConfigurationValue = configurationValue,
                        ConfigurationObject = configurationValue.Exists()
                            ? CreateConfigurationObject(configurationValue)
                            : null,
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

        private static IReadOnlyDictionary<string, string> CreateConfigurationObject(IConfigurationSection section)
        {
            var values = section
                .AsEnumerable(makePathsRelative: true)
                .Where(x => x.Value != null)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value,
                    StringComparer.OrdinalIgnoreCase);

            // Relative enumeration excludes the section's own value. Preserve it under the empty key.
            if (section.Value != null)
            {
                values.Add(string.Empty, section.Value);
            }

            return new ReadOnlyDictionary<string, string>(values);
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
