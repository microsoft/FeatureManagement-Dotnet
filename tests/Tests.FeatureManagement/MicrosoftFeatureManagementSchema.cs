// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

/* Unmerged change from project 'Tests.FeatureManagement(net6.0)'
Before:
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
After:
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.
*/

/* Unmerged change from project 'Tests.FeatureManagement(net7.0)'
Before:
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
After:
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.
*/

/* Unmerged change from project 'Tests.FeatureManagement(net8.0)'
Before:
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
After:
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.
*/
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Xunit;

namespace Tests.FeatureManagement
{
    public class MicrosoftFeatureFlagSchemaTest
    {
        [Fact]
        public async Task ReadsFeatureDefinition()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("MicrosoftFeatureManagement.json").Build();

            var featureDefinitionProvider = new ConfigurationFeatureDefinitionProvider(config);

            FeatureDefinition featureDefinition = await featureDefinitionProvider.GetFeatureDefinitionAsync(Features.OnTestFeature);

            Assert.NotNull(featureDefinition);

            Assert.NotEmpty(featureDefinition.EnabledFor);

            FeatureFilterConfiguration filterConfig = featureDefinition.EnabledFor.First();

            Assert.Equal("AlwaysOn", filterConfig.Name);

            Assert.Equal(RequirementType.Any, featureDefinition.RequirementType);

            featureDefinition = await featureDefinitionProvider.GetFeatureDefinitionAsync(Features.OffTestFeature);

            Assert.NotNull(featureDefinition);

            Assert.Empty(featureDefinition.EnabledFor);

            featureDefinition = await featureDefinitionProvider.GetFeatureDefinitionAsync(Features.AnyFilterFeature);

            Assert.NotNull(featureDefinition);

            Assert.NotEmpty(featureDefinition.EnabledFor);

            Assert.Equal(RequirementType.Any, featureDefinition.RequirementType);

            featureDefinition = await featureDefinitionProvider.GetFeatureDefinitionAsync(Features.AllFilterFeature);

            Assert.NotNull(featureDefinition);

            Assert.NotEmpty(featureDefinition.EnabledFor);

            Assert.Equal(RequirementType.All, featureDefinition.RequirementType);

            featureDefinition = await featureDefinitionProvider.GetFeatureDefinitionAsync(Features.ConditionalFeature);

            Assert.NotNull(featureDefinition);

            Assert.NotEmpty(featureDefinition.EnabledFor);

            filterConfig = featureDefinition.EnabledFor.First();

            Assert.Equal("Test", filterConfig.Name);

            Assert.Equal("V1", filterConfig.Parameters["P1"]);
        }

        [Fact]
        public async Task ReadsMicrosoftFeatureManagementSchemaIfAny()
        {
            string json = @"
            {
              ""AllowedHosts"": ""*"",
              ""feature_management"": {
                ""feature_flags"": [
                  {
                    ""id"": ""FeatureX"",
                    ""enabled"": true
                  }
                ]
              },
              ""FeatureManagement"": {
                 ""FeatureY"": true
              }
            }";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            IConfiguration config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            var services = new ServiceCollection();

            services.AddSingleton(config)
                    .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync("FeatureX"));

            Assert.False(await featureManager.IsEnabledAsync("FeatureY"));
        }
    }
}

