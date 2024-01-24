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

            json = @"
            {
              ""AllowedHosts"": ""*"",
              ""FeatureManagement"": {
                ""FeatureFlags"": [
                  {
                    ""id"": ""Alpha"",
                    ""enabled"": true,
                    ""conditions"": {
                      ""client_filters"": []
                    }
                  },
                  {
                    ""id"": ""Beta"",
                    ""enabled"": true,
                    ""conditions"": {
                      ""client_filters"": [
                        {
                          ""name"": ""Percentage"",
                          ""parameters"": {
                            ""Value"": 100
                           }
                        },
                        {
                          ""name"": ""Targeting"",
                          ""parameters"": {
                            ""Audience"": {
                              ""Users"": [""Jeff""],
                              ""Groups"": [],
                              ""DefaultRolloutPercentage"": 0
                            }
                          }
                        }
					  ],
                      ""requirement_type"" : ""all""
                    }
                  },
                  {
                    ""id"": ""Sigma"",
                    ""enabled"": false,
                    ""conditions"": {
					  ""client_filters"": [
                        {
                          ""name"": ""Percentage"",
                          ""parameters"": {
                            ""Value"": 100
                           }
                        }
			          ]
                    }
                  },
                  {
                    ""id"": ""Omega"",
                    ""enabled"": true,
                    ""conditions"": {
                      ""client_filters"": [
                        {
                          ""name"": ""Percentage"",
                          ""parameters"": {
                            ""Value"": 100
                          }
                        },
                        {
                          ""name"": ""Percentage"",
                          ""parameters"": {
                            ""Value"": 0
                          }
                        }
                      ]
                    }
                  }
                ]
              }
            }";

            stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            services = new ServiceCollection();

            services.AddSingleton(config)
                    .AddFeatureManagement();

            serviceProvider = services.BuildServiceProvider();

            featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync("Alpha"));

            Assert.True(await featureManager.IsEnabledAsync("Beta", new TargetingContext
            {
                UserId = "Jeff"
            }));

            Assert.False(await featureManager.IsEnabledAsync("Beta", new TargetingContext
            {
                UserId = "Sam"
            }));

            Assert.False(await featureManager.IsEnabledAsync("Sigma"));

            Assert.True(await featureManager.IsEnabledAsync("Omega"));
        }
    }
}
