// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
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

            featureDefinition = await featureDefinitionProvider.GetFeatureDefinitionAsync(Features.AlwaysOnTestFeature);

            Assert.NotNull(featureDefinition);

            Assert.True(featureDefinition.Telemetry.Enabled);

            Assert.Equal("Tag1Value", featureDefinition.Telemetry.Metadata["Tags.Tag1"]);

            Assert.Equal("Tag2Value", featureDefinition.Telemetry.Metadata["Tags.Tag2"]);

            Assert.Equal("EtagValue", featureDefinition.Telemetry.Metadata["Etag"]);

            Assert.Equal("LabelValue", featureDefinition.Telemetry.Metadata["Label"]);

            featureDefinition = await featureDefinitionProvider.GetFeatureDefinitionAsync(Features.VariantTestFeature);

            Assert.NotNull(featureDefinition);

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

