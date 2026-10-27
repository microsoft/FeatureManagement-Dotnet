// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.FeatureManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests.FeatureManagement
{
    public class VariantExtensionsTest
    {
        private const string CachedFeatureName = "CachedVariantFeature";
        private const string CachedVariantName = "CachedVariant";

        private const string ConfigurationValuePath =
            "feature_management:feature_flags:0:variants:0:configuration_value:Value";

        [Fact]
        public void GetConfigurationReturnsNullForNullVariant()
        {
            Variant variant = null;

            Assert.Null(variant.GetConfiguration<string>());
        }

        [Fact]
        public void GetConfigurationPrefersAssignableObject()
        {
            var supplied = new List<string> { "supplied" };

            var provider = new MemoryConfigurationProvider(new MemoryConfigurationSource());
            var configurationSection = new ConfigurationSection(
                new ConfigurationRoot(new List<IConfigurationProvider> { provider }),
                "Param");
            provider.Set(configurationSection.Key, "42");
            var variant = new Variant
            {
                ConfigurationObject = supplied,
                Configuration = configurationSection
            };

            Assert.Same(supplied, variant.GetConfiguration<IReadOnlyList<string>>());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetConfigurationFallsBackToSection(bool hasIncompatibleObject)
        {
            var provider = new MemoryConfigurationProvider(new MemoryConfigurationSource
            {
                InitialData = new Dictionary<string, string>
                {
                    ["Value:AccountId"] = "1",
                    ["Value:UserId"] = "2",
                    ["Value:Groups:0"] = "Chrome",
                    ["Value:Groups:1"] = "Edge",
                }
            });
            var configurationSection = new ConfigurationSection(
                new ConfigurationRoot(new List<IConfigurationProvider> { provider }),
                "Value");
            var variant = new Variant
            {
                ConfigurationObject = hasIncompatibleObject
                    ? "some value"
                    : null,
                Configuration = configurationSection
            };

            AppContext firstConfiguration = variant.GetConfiguration<AppContext>();
            AppContext secondConfiguration = variant.GetConfiguration<AppContext>();

            Assert.Equivalent(new AppContext
            {
                AccountId = "1",
                UserId = "2",
                Groups = new List<string> { "Chrome", "Edge" }
            }, firstConfiguration);
            Assert.NotSame(firstConfiguration, secondConfiguration);
        }

        [Fact]
        public async Task GetConfigurationCachesProviderConfigurationByTypeAcrossVariants()
        {
            var configuration = CreateVariantConfiguration("initial");
            using var provider = new ConfigurationFeatureDefinitionProvider(configuration);
            var featureManager = new FeatureManager(provider);

            Variant firstVariant = await featureManager.GetVariantAsync(CachedFeatureName);
            Variant secondVariant = await featureManager.GetVariantAsync(CachedFeatureName);

            CachedVariantConfiguration firstConfiguration = firstVariant.GetConfiguration<CachedVariantConfiguration>();
            AlternateCachedVariantConfiguration alternateConfiguration =
                firstVariant.GetConfiguration<AlternateCachedVariantConfiguration>();

            Assert.NotSame(firstVariant, secondVariant);
            Assert.Equal("initial", firstConfiguration.Value);
            Assert.Equal("initial", alternateConfiguration.Value);
            Assert.Same(firstConfiguration, firstVariant.GetConfiguration<CachedVariantConfiguration>());
            Assert.Same(firstConfiguration, secondVariant.GetConfiguration<CachedVariantConfiguration>());
            Assert.Same(alternateConfiguration, secondVariant.GetConfiguration<AlternateCachedVariantConfiguration>());
            Assert.NotSame(firstConfiguration, alternateConfiguration);
        }

        [Fact]
        public async Task GetConfigurationUsesNewCacheAfterConfigurationReload()
        {
            var configuration = CreateVariantConfiguration("before");
            using var provider = new ConfigurationFeatureDefinitionProvider(configuration);
            var featureManager = new FeatureManager(provider);

            Variant beforeVariant = await featureManager.GetVariantAsync(CachedFeatureName);
            CachedVariantConfiguration beforeConfiguration =
                beforeVariant.GetConfiguration<CachedVariantConfiguration>();

            configuration.Providers.Last().Set(ConfigurationValuePath, "after");
            configuration.Reload();

            Variant afterVariant = await featureManager.GetVariantAsync(CachedFeatureName);
            CachedVariantConfiguration afterConfiguration = afterVariant.GetConfiguration<CachedVariantConfiguration>();

            Assert.Equal("before", beforeConfiguration.Value);
            Assert.Equal("after", afterConfiguration.Value);
            Assert.NotSame(beforeConfiguration, afterConfiguration);
            Assert.Same(beforeConfiguration, beforeVariant.GetConfiguration<CachedVariantConfiguration>());
            Assert.Same(afterConfiguration, afterVariant.GetConfiguration<CachedVariantConfiguration>());
        }

        [Fact]
        public async Task GetConfigurationBypassesProviderCacheAfterConfigurationIsReplaced()
        {
            var configuration = CreateVariantConfiguration("initial");
            using var provider = new ConfigurationFeatureDefinitionProvider(configuration);
            var featureManager = new FeatureManager(provider);
            Variant variant = await featureManager.GetVariantAsync(CachedFeatureName);
            CachedVariantConfiguration initialConfiguration = variant.GetConfiguration<CachedVariantConfiguration>();
            var replacementConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["replacement:Value"] = "replacement"
                })
                .Build();

            variant.Configuration = replacementConfiguration.GetSection("replacement");

            CachedVariantConfiguration firstReplacement = variant.GetConfiguration<CachedVariantConfiguration>();
            CachedVariantConfiguration secondReplacement = variant.GetConfiguration<CachedVariantConfiguration>();

            Assert.Equal("initial", initialConfiguration.Value);
            Assert.Equal("replacement", firstReplacement.Value);
            Assert.Equal("replacement", secondReplacement.Value);
            Assert.NotSame(initialConfiguration, firstReplacement);
            Assert.NotSame(firstReplacement, secondReplacement);
        }

        [Fact]
        public async Task GetConfigurationBypassesProviderCacheAfterDefinitionConfigurationIsReplaced()
        {
            var configuration = CreateVariantConfiguration("initial");
            using var provider = new ConfigurationFeatureDefinitionProvider(configuration);
            FeatureDefinition definition = await provider.GetFeatureDefinitionAsync(CachedFeatureName);
            VariantDefinition variantDefinition = Assert.Single(definition.Variants);
            var replacementConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["replacement:Value"] = "replacement"
                })
                .Build();
            IConfigurationSection replacementSection = replacementConfiguration.GetSection("replacement");

            variantDefinition.ConfigurationValue = replacementSection;

            var featureManager = new FeatureManager(provider);
            Variant variant = await featureManager.GetVariantAsync(CachedFeatureName);

            Assert.Same(replacementSection, variant.Configuration);
            Assert.Null(variant.ConfigurationCache);
            Assert.Equal("replacement", variant.GetConfiguration<CachedVariantConfiguration>().Value);
        }

        [Fact]
        public void GetConfigurationBindsScalar()
        {
            var provider = new MemoryConfigurationProvider(new MemoryConfigurationSource());
            var configurationSection = new ConfigurationSection(
                new ConfigurationRoot(new List<IConfigurationProvider> { provider }),
                "Param");
            provider.Set(configurationSection.Key, "42");
            var variant = new Variant
            {
                Configuration = configurationSection
            };

            Assert.Equal(42, variant.GetConfiguration<int>());
        }

        [Fact]
        public void GetConfigurationReturnsNullWithoutConfiguration()
        {
            Assert.Null(new Variant().GetConfiguration<string>());
            Assert.Null(new Variant().GetConfiguration<int?>());
        }

        private static IConfigurationRoot CreateVariantConfiguration(string value)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["feature_management:feature_flags:0:id"] = CachedFeatureName,
                    ["feature_management:feature_flags:0:enabled"] = bool.TrueString,
                    ["feature_management:feature_flags:0:variants:0:name"] = CachedVariantName,
                    [ConfigurationValuePath] = value,
                    ["feature_management:feature_flags:0:allocation:default_when_enabled"] = CachedVariantName
                })
                .Build();
        }

        private sealed class CachedVariantConfiguration
        {
            public string Value { get; set; }
        }

        private sealed class AlternateCachedVariantConfiguration
        {
            public string Value { get; set; }
        }
    }
}
