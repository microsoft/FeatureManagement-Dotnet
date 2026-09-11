// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.FeatureManagement;
using System.Collections.Generic;
using Xunit;

namespace Tests.FeatureManagement
{
    public class VariantExtensionsTest
    {
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

            Assert.Equivalent(new AppContext
            {
                AccountId = "1",
                UserId = "2",
                Groups = new List<string> { "Chrome", "Edge" }
            }, variant.GetConfiguration<AppContext>());
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
    }
}
