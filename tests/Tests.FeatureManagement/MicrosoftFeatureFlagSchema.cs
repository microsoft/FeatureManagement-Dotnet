// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tests.FeatureManagement
{
    public class MicrosoftFeatureFlagSchemaTest
    {
        [Fact]
        public async Task ReadsMicrosoftFeatureFlagSchema()
        {
            string json = @"
            {
              ""AllowedHosts"": ""*"",
              ""FeatureManagement"": {
                ""MyFeature"": true,
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

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            IConfiguration config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            var services = new ServiceCollection();

            services.AddSingleton(config)
                    .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.False(await featureManager.IsEnabledAsync("MyFeature"));

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

            json = @"
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

            stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            services = new ServiceCollection();

            services.AddFeatureManagement(config.GetSection("FeatureManagement"));

            serviceProvider = services.BuildServiceProvider();

            featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

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
    }
}
