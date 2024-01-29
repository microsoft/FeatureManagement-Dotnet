// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tests.FeatureManagement
{
    public class MicrosoftFeatureFlagSchemaTest
    {
        [Fact]
        public async Task ReadsMicrosoftFeatureFlagSchemaIfAny()
        {
            string json = @"
            {
              ""AllowedHosts"": ""*"",
              ""FeatureManagement"": {
                ""MyFeature"": true,
                ""FeatureFlags"": [
                  {
                    ""id"": ""Alpha"",
                    ""enabled"": true
                  }
                ]
              }
            }";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            IConfiguration config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            var services = new ServiceCollection();

            services.AddSingleton(config)
                    .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.False(await featureManager.IsEnabledAsync("MyFeature"));

            Assert.True(await featureManager.IsEnabledAsync("Alpha"));

            json = @"
            {
              ""AllowedHosts"": ""*"",
              ""FeatureManagement"": {
                ""MyFeature"": true,
                ""FeatureFlags"": true
              }
            }";

            stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            services = new ServiceCollection();

            services.AddSingleton(config)
                    .AddFeatureManagement();

            serviceProvider = services.BuildServiceProvider();

            featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync("MyFeature"));

            Assert.True(await featureManager.IsEnabledAsync("FeatureFlags"));

            json = @"
            {
              ""AllowedHosts"": ""*"",
              ""FeatureManagement"": {
                ""MyFeature"": true,
                ""FeatureFlags"": {
                  ""EnabledFor"": [
                    {
                      ""Name"": ""AlwaysOn""  
                    }
                  ]
                }
              }
            }";

            stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            services = new ServiceCollection();

            services.AddSingleton(config)
                    .AddFeatureManagement();

            serviceProvider = services.BuildServiceProvider();

            featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync("MyFeature"));

            Assert.True(await featureManager.IsEnabledAsync("FeatureFlags"));
        }

        [Fact]
        public async Task ReadsTopLevelConfiguration()
        {
            string json = @"
            {
              ""AllowedHosts"": ""*"",
              ""FeatureManagement"": {
                ""MyFeature"": true,
                ""FeatureFlags"": [
                  {
                    ""id"": ""Alpha"",
                    ""enabled"": true
                  }
                ]
              }
            }";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            IConfiguration config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            var services = new ServiceCollection();

            services.AddFeatureManagement(config.GetSection("FeatureManagement"));

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.False(await featureManager.IsEnabledAsync("MyFeature"));

            Assert.True(await featureManager.IsEnabledAsync("Alpha"));

            json = @"
            {
              ""AllowedHosts"": ""*"",
              ""FeatureManagement"": {
                ""MyFeature"": true,
                ""FeatureFlags"": true
              }
            }";

            stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            services = new ServiceCollection();

            services.AddFeatureManagement(config.GetSection("FeatureManagement"));

            serviceProvider = services.BuildServiceProvider();

            featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync("MyFeature"));

            Assert.True(await featureManager.IsEnabledAsync("FeatureFlags"));

            json = @"
            {
              ""AllowedHosts"": ""*"",
              ""FeatureManagement"": {
                ""MyFeature"": true,
                ""FeatureFlags"": {
                  ""EnabledFor"": [
                    {
                      ""Name"": ""AlwaysOn""  
                    }
                  ]
                }
              }
            }";

            stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            services = new ServiceCollection();

            services.AddFeatureManagement(config.GetSection("FeatureManagement"));

            serviceProvider = services.BuildServiceProvider();

            featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync("MyFeature"));

            Assert.True(await featureManager.IsEnabledAsync("FeatureFlags"));
        }

        [Fact]
        public async Task ReadsFeatureDefinition()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("MicrosoftFeatureFlag.json").Build();

            var featureDefinitionProvider = new ConfigurationFeatureDefinitionProvider(config);

            FeatureDefinition featureDefinition = await featureDefinitionProvider.GetFeatureDefinitionAsync(Features.AlwaysOnTestFeature);

            Assert.NotNull(featureDefinition);

            Assert.Equal(RequirementType.All, featureDefinition.RequirementType);

            Assert.Equal(FeatureStatus.Conditional, featureDefinition.Status);

            Assert.Equal("Small", featureDefinition.Allocation.DefaultWhenEnabled);

            Assert.Equal("Big", featureDefinition.Allocation.DefaultWhenDisabled);

            Assert.Equal("Small", featureDefinition.Allocation.User.First().Variant);

            Assert.Equal("Jeff", featureDefinition.Allocation.User.First().Users.First());

            Assert.Equal("Big", featureDefinition.Allocation.Group.First().Variant);

            Assert.Equal("Group1", featureDefinition.Allocation.Group.First().Groups.First());

            Assert.Equal("Small", featureDefinition.Allocation.Percentile.First().Variant);

            Assert.Equal(0, featureDefinition.Allocation.Percentile.First().From);

            Assert.Equal(50, featureDefinition.Allocation.Percentile.First().To);

            Assert.Equal("12345", featureDefinition.Allocation.Seed);

            VariantDefinition smallVariant = featureDefinition.Variants.FirstOrDefault(variant => string.Equals(variant.Name, "Small"));

            Assert.NotNull(smallVariant);

            Assert.Equal("300px", smallVariant.ConfigurationValue.Value);

            Assert.Equal(StatusOverride.None, smallVariant.StatusOverride);

            VariantDefinition bigVariant = featureDefinition.Variants.FirstOrDefault(variant => string.Equals(variant.Name, "Big"));

            Assert.NotNull(bigVariant);

            Assert.Equal("ShoppingCart:Big", bigVariant.ConfigurationReference);

            Assert.Equal(StatusOverride.Disabled, bigVariant.StatusOverride);
        }

        [Fact]
        public async Task ReadsFeatureFilterConfiguration()
        {
            string json = @"
            {
              ""AllowedHosts"": ""*"",
              ""FeatureManagement"": {
                ""FeatureFlags"": [
                  {
                    ""id"": ""ConditionalFeature"",
                    ""enabled"": true,
                    ""conditions"": {
                      ""client_filters"": [
                        {
                          ""name"": ""Test"",
                          ""parameters"": {
                            ""P1"": ""V1""
                           }
                        }
					  ]
                    }
                  },
                ]
              }
            }";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            IConfiguration config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            var services = new ServiceCollection();

            services.AddSingleton(config)
                    .AddFeatureManagement()
                    .AddFeatureFilter<TestFilter>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            IEnumerable<IFeatureFilterMetadata> featureFilters = serviceProvider.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>();

            //
            // Sync filter
            TestFilter testFeatureFilter = (TestFilter)featureFilters.First(f => f is TestFilter);

            bool called = false;

            testFeatureFilter.Callback = (evaluationContext) =>
            {
                called = true;

                Assert.Equal("V1", evaluationContext.Parameters["P1"]);

                Assert.Equal(Features.ConditionalFeature, evaluationContext.FeatureName);

                return Task.FromResult(true);
            };

            await featureManager.IsEnabledAsync(Features.ConditionalFeature);

            Assert.True(called);
        }
    }
}
