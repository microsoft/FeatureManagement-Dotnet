// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
namespace ParameterObjectConsoleApp
{
    /// <summary>
    /// A custom feature definition provider that supplies targeting filter settings
    /// directly using the ParametersObject property, avoiding the need to construct IConfiguration.
    /// </summary>
    public class InMemoryFeatureDefinitionProvider : IFeatureDefinitionProvider
    {
        private readonly Dictionary<string, FeatureDefinition> _featureDefinitions;

        public InMemoryFeatureDefinitionProvider()
        {
            //
            // Define feature flags with targeting settings.
            // This demonstrates supplying filter parameters directly via ParametersObject
            // instead of constructing an IConfiguration with key-value pairs.
            _featureDefinitions = new Dictionary<string, FeatureDefinition>
            {
                ["Beta"] = new FeatureDefinition
                {
                    Name = "Beta",
                    EnabledFor = new List<FeatureFilterConfiguration>
                    {
                        new FeatureFilterConfiguration
                        {
                            Name = "Microsoft.Targeting",
                            ParametersObject = new TargetingFilterSettings
                            {
                                Audience = new Audience
                                {
                                    Users = new List<string> { "Jeff", "Anne" },
                                    Groups = new List<GroupRollout>
                                    {
                                        new GroupRollout
                                        {
                                            Name = "Management",
                                            RolloutPercentage = 100
                                        },
                                        new GroupRollout
                                        {
                                            Name = "TeamMembers",
                                            RolloutPercentage = 45
                                        }
                                    },
                                    DefaultRolloutPercentage = 20
                                }
                            }
                        }
                    }
                }
            };
        }

        public Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            _featureDefinitions.TryGetValue(featureName, out FeatureDefinition definition);

            return Task.FromResult(definition);
        }

        public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
        {
            foreach (var definition in _featureDefinitions.Values)
            {
                yield return definition;
            }

            await Task.CompletedTask;
        }
    }
}
