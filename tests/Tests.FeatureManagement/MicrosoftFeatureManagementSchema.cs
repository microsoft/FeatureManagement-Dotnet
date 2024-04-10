// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement.Telemetry;
using Microsoft.FeatureManagement.Tests;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

            featureDefinition = await featureDefinitionProvider.GetFeatureDefinitionAsync(Features.VariantFeatureDefaultEnabled);

            Assert.NotNull(featureDefinition);

            Assert.Equal("Medium", featureDefinition.Allocation.DefaultWhenEnabled);

            Assert.Equal("Small", featureDefinition.Allocation.User.First().Variant);

            Assert.Equal("Jeff", featureDefinition.Allocation.User.First().Users.First());

            VariantDefinition smallVariant = featureDefinition.Variants.FirstOrDefault(variant => string.Equals(variant.Name, "Small"));

            Assert.NotNull(smallVariant);

            Assert.Equal("300px", smallVariant.ConfigurationValue.Value);

            Assert.Equal(StatusOverride.None, smallVariant.StatusOverride);

            featureDefinition = await featureDefinitionProvider.GetFeatureDefinitionAsync(Features.VariantFeatureStatusDisabled);

            Assert.NotNull(featureDefinition);

            Assert.Equal("Small", featureDefinition.Allocation.DefaultWhenDisabled);

            featureDefinition = await featureDefinitionProvider.GetFeatureDefinitionAsync(Features.VariantFeaturePercentileOn);

            Assert.NotNull(featureDefinition);

            Assert.Equal(0, featureDefinition.Allocation.Percentile.First().From);

            Assert.Equal(50, featureDefinition.Allocation.Percentile.First().To);

            Assert.Equal("Big", featureDefinition.Allocation.Percentile.First().Variant);

            Assert.Equal("1234", featureDefinition.Allocation.Seed);

            VariantDefinition bigVariant = featureDefinition.Variants.FirstOrDefault(variant => string.Equals(variant.Name, "Big"));

            Assert.NotNull(bigVariant);

            Assert.Equal("ShoppingCart:Big", bigVariant.ConfigurationReference);

            Assert.Equal(StatusOverride.Disabled, bigVariant.StatusOverride);

            featureDefinition = await featureDefinitionProvider.GetFeatureDefinitionAsync(Features.VariantFeatureGroup);

            Assert.NotNull(featureDefinition);

            Assert.Equal("Small", featureDefinition.Allocation.Group.First().Variant);

            Assert.Equal("Group1", featureDefinition.Allocation.Group.First().Groups.First());
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

        [Fact]
        public async Task TelemetryPublishing()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("MicrosoftFeatureManagement.json").Build();

            var services = new ServiceCollection();

            var targetingContextAccessor = new OnDemandTargetingContextAccessor();
            services.AddSingleton<ITargetingContextAccessor>(targetingContextAccessor)
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddTelemetryPublisher<TestTelemetryPublisher>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            FeatureManager featureManager = (FeatureManager)serviceProvider.GetRequiredService<IVariantFeatureManager>();
            TestTelemetryPublisher testPublisher = (TestTelemetryPublisher)featureManager.TelemetryPublishers.First();
            CancellationToken cancellationToken = CancellationToken.None;

            // Test a feature with telemetry disabled
            bool result = await featureManager.IsEnabledAsync(Features.OnTestFeature, cancellationToken);

            Assert.True(result);
            Assert.Null(testPublisher.evaluationEventCache);

            // Test telemetry cases
            result = await featureManager.IsEnabledAsync(Features.AlwaysOnTestFeature, cancellationToken);

            Assert.True(result);
            Assert.Equal(Features.AlwaysOnTestFeature, testPublisher.evaluationEventCache.FeatureDefinition.Name);
            Assert.Equal(result, testPublisher.evaluationEventCache.Enabled);
            Assert.Equal("EtagValue", testPublisher.evaluationEventCache.FeatureDefinition.Telemetry.Metadata["Etag"]);
            Assert.Equal("LabelValue", testPublisher.evaluationEventCache.FeatureDefinition.Telemetry.Metadata["Label"]);
            Assert.Equal("Tag1Value", testPublisher.evaluationEventCache.FeatureDefinition.Telemetry.Metadata["Tags.Tag1"]);
            Assert.Null(testPublisher.evaluationEventCache.Variant);
            Assert.Equal(VariantAssignmentReason.None, testPublisher.evaluationEventCache.VariantAssignmentReason);

            // Test variant cases
            result = await featureManager.IsEnabledAsync(Features.VariantFeatureDefaultEnabled, cancellationToken);

            Assert.True(result);
            Assert.Equal(Features.VariantFeatureDefaultEnabled, testPublisher.evaluationEventCache.FeatureDefinition.Name);
            Assert.Equal(result, testPublisher.evaluationEventCache.Enabled);
            Assert.Equal("Medium", testPublisher.evaluationEventCache.Variant.Name);

            Variant variantResult = await featureManager.GetVariantAsync(Features.VariantFeatureDefaultEnabled, cancellationToken);

            Assert.True(testPublisher.evaluationEventCache.Enabled);
            Assert.Equal(Features.VariantFeatureDefaultEnabled, testPublisher.evaluationEventCache.FeatureDefinition.Name);
            Assert.Equal(variantResult.Name, testPublisher.evaluationEventCache.Variant.Name);
            Assert.Equal(VariantAssignmentReason.DefaultWhenEnabled, testPublisher.evaluationEventCache.VariantAssignmentReason);

            result = await featureManager.IsEnabledAsync(Features.VariantFeatureStatusDisabled, cancellationToken);

            Assert.False(result);
            Assert.Equal(Features.VariantFeatureStatusDisabled, testPublisher.evaluationEventCache.FeatureDefinition.Name);
            Assert.Equal(result, testPublisher.evaluationEventCache.Enabled);
            Assert.Equal("Small", testPublisher.evaluationEventCache.Variant.Name);
            Assert.Equal(VariantAssignmentReason.DefaultWhenDisabled, testPublisher.evaluationEventCache.VariantAssignmentReason);

            variantResult = await featureManager.GetVariantAsync(Features.VariantFeatureStatusDisabled, cancellationToken);

            Assert.False(testPublisher.evaluationEventCache.Enabled);
            Assert.Equal(Features.VariantFeatureStatusDisabled, testPublisher.evaluationEventCache.FeatureDefinition.Name);
            Assert.Equal(variantResult.Name, testPublisher.evaluationEventCache.Variant.Name);
            Assert.Equal(VariantAssignmentReason.DefaultWhenDisabled, testPublisher.evaluationEventCache.VariantAssignmentReason);

            targetingContextAccessor.Current = new TargetingContext
            {
                UserId = "Marsha",
                Groups = new List<string> { "Group1" }
            };

            variantResult = await featureManager.GetVariantAsync(Features.VariantFeaturePercentileOn, cancellationToken);
            Assert.Equal("Big", variantResult.Name);
            Assert.Equal("Big", testPublisher.evaluationEventCache.Variant.Name);
            Assert.Equal("Marsha", testPublisher.evaluationEventCache.TargetingContext.UserId);
            Assert.Equal(VariantAssignmentReason.Percentile, testPublisher.evaluationEventCache.VariantAssignmentReason);

            variantResult = await featureManager.GetVariantAsync(Features.VariantFeaturePercentileOff, cancellationToken);
            Assert.Null(variantResult);
            Assert.Null(testPublisher.evaluationEventCache.Variant);
            Assert.Equal(VariantAssignmentReason.DefaultWhenEnabled, testPublisher.evaluationEventCache.VariantAssignmentReason);

            variantResult = await featureManager.GetVariantAsync(Features.VariantFeatureAlwaysOff, cancellationToken);
            Assert.Null(variantResult);
            Assert.Null(testPublisher.evaluationEventCache.Variant);
            Assert.Equal(VariantAssignmentReason.DefaultWhenDisabled, testPublisher.evaluationEventCache.VariantAssignmentReason);

            variantResult = await featureManager.GetVariantAsync(Features.VariantFeatureUser, cancellationToken);
            Assert.Equal("Small", variantResult.Name);
            Assert.Equal("Small", testPublisher.evaluationEventCache.Variant.Name);
            Assert.Equal(VariantAssignmentReason.User, testPublisher.evaluationEventCache.VariantAssignmentReason);

            variantResult = await featureManager.GetVariantAsync(Features.VariantFeatureGroup, cancellationToken);
            Assert.Equal("Small", variantResult.Name);
            Assert.Equal("Small", testPublisher.evaluationEventCache.Variant.Name);
            Assert.Equal(VariantAssignmentReason.Group, testPublisher.evaluationEventCache.VariantAssignmentReason);

            variantResult = await featureManager.GetVariantAsync(Features.VariantFeatureNoAllocation, cancellationToken);
            Assert.Null(variantResult);
            Assert.Null(testPublisher.evaluationEventCache.Variant);
            Assert.Equal(VariantAssignmentReason.DefaultWhenEnabled, testPublisher.evaluationEventCache.VariantAssignmentReason);

            variantResult = await featureManager.GetVariantAsync(Features.VariantFeatureAlwaysOffNoAllocation, cancellationToken);
            Assert.Null(variantResult);
            Assert.Null(testPublisher.evaluationEventCache.Variant);
            Assert.Equal(VariantAssignmentReason.DefaultWhenDisabled, testPublisher.evaluationEventCache.VariantAssignmentReason);
        }

        [Fact]
        public async Task UsesVariants()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("MicrosoftFeatureManagement.json").Build();

            var services = new ServiceCollection();

            var targetingContextAccessor = new OnDemandTargetingContextAccessor();
            services.AddSingleton<ITargetingContextAccessor>(targetingContextAccessor)
                    .AddSingleton(config)
                    .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IVariantFeatureManager featureManager = serviceProvider.GetRequiredService<IVariantFeatureManager>();
            CancellationToken cancellationToken = CancellationToken.None;

            targetingContextAccessor.Current = new TargetingContext
            {
                UserId = "Marsha",
                Groups = new List<string> { "Group1" }
            };

            // Test StatusOverride and Percentile with Seed
            Variant variant = await featureManager.GetVariantAsync(Features.VariantFeaturePercentileOn, cancellationToken);

            Assert.Equal("Big", variant.Name);
            Assert.Equal("green", variant.Configuration["Color"]);
            Assert.False(await featureManager.IsEnabledAsync(Features.VariantFeaturePercentileOn, cancellationToken));

            variant = await featureManager.GetVariantAsync(Features.VariantFeaturePercentileOff, cancellationToken);

            Assert.Null(variant);
            Assert.True(await featureManager.IsEnabledAsync(Features.VariantFeaturePercentileOff, cancellationToken));

            // Test Status = Disabled
            variant = await featureManager.GetVariantAsync(Features.VariantFeatureStatusDisabled, cancellationToken);

            Assert.Equal("Small", variant.Name);
            Assert.Equal("300px", variant.Configuration.Value);
            Assert.False(await featureManager.IsEnabledAsync(Features.VariantFeatureStatusDisabled, cancellationToken));

            // Test DefaultWhenEnabled and ConfigurationValue with inline IConfigurationSection
            variant = await featureManager.GetVariantAsync(Features.VariantFeatureDefaultEnabled, cancellationToken);

            Assert.Equal("Medium", variant.Name);
            Assert.Equal("450px", variant.Configuration["Size"]);
            Assert.True(await featureManager.IsEnabledAsync(Features.VariantFeatureDefaultEnabled, cancellationToken));

            // Test User allocation
            variant = await featureManager.GetVariantAsync(Features.VariantFeatureUser, cancellationToken);

            Assert.Equal("Small", variant.Name);
            Assert.Equal("300px", variant.Configuration.Value);
            Assert.True(await featureManager.IsEnabledAsync(Features.VariantFeatureUser, cancellationToken));

            // Test Group allocation
            variant = await featureManager.GetVariantAsync(Features.VariantFeatureGroup, cancellationToken);

            Assert.Equal("Small", variant.Name);
            Assert.Equal("300px", variant.Configuration.Value);
            Assert.True(await featureManager.IsEnabledAsync(Features.VariantFeatureGroup, cancellationToken));
        }

        [Fact]
        public async Task VariantsInvalidScenarios()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("MicrosoftFeatureManagement.json").Build();

            var services = new ServiceCollection();

            var targetingContextAccessor = new OnDemandTargetingContextAccessor();
            services.AddSingleton<ITargetingContextAccessor>(targetingContextAccessor)
                    .AddSingleton(config)
                    .AddFeatureManagement();

            targetingContextAccessor.Current = new TargetingContext
            {
                UserId = "Jeff"
            };

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IVariantFeatureManager featureManager = serviceProvider.GetRequiredService<IVariantFeatureManager>();
            CancellationToken cancellationToken = CancellationToken.None;

            // Verify null variant returned if no variants are specified
            Variant variant = await featureManager.GetVariantAsync(Features.VariantFeatureNoVariants, cancellationToken);

            Assert.Null(variant);

            // Verify null variant returned if no allocation is specified
            variant = await featureManager.GetVariantAsync(Features.VariantFeatureNoAllocation, cancellationToken);

            Assert.Null(variant);

            // Verify that ConfigurationValue has priority over ConfigurationReference
            variant = await featureManager.GetVariantAsync(Features.VariantFeatureBothConfigurations, cancellationToken);

            Assert.Equal("600px", variant.Configuration.Value);

            // Verify that an exception is thrown for invalid StatusOverride value
            FeatureManagementException e = await Assert.ThrowsAsync<FeatureManagementException>(async () =>
            {
                variant = await featureManager.GetVariantAsync(Features.VariantFeatureInvalidStatusOverride, cancellationToken);
            });

            Assert.Equal(FeatureManagementError.InvalidConfigurationSetting, e.Error);
            Assert.Contains(MicrosoftFeatureManagementFields.VariantDefinitionStatusOverride, e.Message);

            // Verify that an exception is thrown for invalid doubles From and To in the Percentile section
            e = await Assert.ThrowsAsync<FeatureManagementException>(async () =>
            {
                variant = await featureManager.GetVariantAsync(Features.VariantFeatureInvalidFromTo, cancellationToken);
            });

            Assert.Equal(FeatureManagementError.InvalidConfigurationSetting, e.Error);
            Assert.Contains(MicrosoftFeatureManagementFields.PercentileAllocationFrom, e.Message);
        }
    }
}

