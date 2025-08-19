// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement.Telemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.FeatureManagement
{
    public class DotnetSchemaFeatureManagementBasicFunctionTest
    {
        [Fact]
        public async Task ReadsConfiguration()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("DotnetFeatureManagementSchema.json").Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<TestFilter>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync(Features.OnTestFeature));

            Assert.False(await featureManager.IsEnabledAsync(Features.OffTestFeature));

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

            bool hasItems = false;

            await foreach (string feature in featureManager.GetFeatureNamesAsync())
            {
                hasItems = true;

                break;
            }

            Assert.True(hasItems);
        }

        [Fact]
        public async Task ReadsTopLevelConfiguration()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes($"{{\"AllowedHosts\": \"*\", \"FeatureFlags\": {{\"FeatureX\": true}}}}"));

            IConfiguration config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            var services = new ServiceCollection();

            services.AddFeatureManagement(config.GetSection("FeatureFlags"));

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync("FeatureX"));

            string json = @"
            {
              ""FeatureFlags"": {
                ""FeatureX"": true,
                ""feature_management"": {
                  ""feature_flags"": [
                    {
                      ""id"": ""FeatureY"",
                      ""enabled"": true
                    }
                  ]
                }
              }
            }";

            stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            services = new ServiceCollection();

            services.AddFeatureManagement(config.GetSection("FeatureFlags"));

            serviceProvider = services.BuildServiceProvider();

            featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            // If Microsoft schema can be found, it will not fall back to root configuration.
            Assert.False(await featureManager.IsEnabledAsync("FeatureX"));

            Assert.True(await featureManager.IsEnabledAsync("FeatureY"));
        }
    }

    public class FeatureManagementBasicFunctionTest
    {
        [Fact]
        public async Task ReadsConfiguration()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<TestFilter>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync(Features.OnTestFeature));

            Assert.False(await featureManager.IsEnabledAsync(Features.OffTestFeature));

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

            bool hasItems = false;

            await foreach (string feature in featureManager.GetFeatureNamesAsync())
            {
                hasItems = true;

                break;
            }

            Assert.True(hasItems);

            Assert.False(await featureManager.IsEnabledAsync("NonExistentFeature"));
        }

        [Fact]
        public async Task ReadsOnlyFeatureManagementSection()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"AllowedHosts\": \"*\"}"));

            IConfiguration config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            await foreach (string featureName in featureManager.GetFeatureNamesAsync())
            {
                //
                // Fail, as no features should be found
                Assert.True(false);
            }
        }

        [Fact]
        public async Task RespectsAllFeatureManagementSchemas()
        {
            string json = @"
            {
              ""AllowedHosts"": ""*"",
              ""FeatureManagement"": {
                 ""FeatureX"": true,
                 ""FeatureY"": true
              },
              ""feature_management"": {
                ""feature_flags"": [
                  {
                    ""id"": ""FeatureZ"",
                    ""enabled"": true
                  },
                  {
                    ""id"": ""FeatureY"",
                    ""enabled"": false
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

            Assert.True(await featureManager.IsEnabledAsync("FeatureX"));

            // feature flag written in Microsoft schema has higher priority
            Assert.False(await featureManager.IsEnabledAsync("FeatureY"));

            Assert.True(await featureManager.IsEnabledAsync("FeatureZ"));
        }

        [Fact]
        public async Task ThrowsForMissingFeatures()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .Configure<FeatureManagementOptions>(options =>
                {
                    options.IgnoreMissingFeatures = false;
                });

            services
                .AddSingleton(config)
                .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            FeatureManagementException fme = await Assert.ThrowsAsync<FeatureManagementException>(() =>
                featureManager.IsEnabledAsync("NonExistentFeature"));
        }

        [Fact]
        public async Task ThreadSafeSnapshot()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<TestFilter>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManagerSnapshot>();

            IEnumerable<IFeatureFilterMetadata> featureFilters = serviceProvider.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>();

            //
            // Sync filter
            TestFilter testFeatureFilter = (TestFilter)featureFilters.First(f => f is TestFilter);

            bool called = false;

            testFeatureFilter.Callback = async (evaluationContext) =>
            {
                called = true;

                await Task.Delay(10);

                return new Random().Next(0, 100) > 50;
            };

            var tasks = new List<Task<bool>>();

            for (int i = 0; i < 1000; i++)
            {
                tasks.Add(featureManager.IsEnabledAsync(Features.ConditionalFeature));
            }

            Assert.True(called);

            await Task.WhenAll(tasks);

            bool result = await tasks.First();

            foreach (Task<bool> t in tasks)
            {
                Assert.Equal(result, await t);
            }
        }

        [Fact]
        public void AddsScopedFeatureManagement()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddScopedFeatureManagement()
                .WithTargeting<OnDemandTargetingContextAccessor>()
                .AddFeatureFilter<TestFilter>();

            Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IFeatureDefinitionProvider) && descriptor.Lifetime == ServiceLifetime.Singleton);
            Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IFeatureDefinitionProvider) && descriptor.Lifetime == ServiceLifetime.Scoped);

            Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IFeatureManager) && descriptor.Lifetime == ServiceLifetime.Scoped);
            Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IFeatureManager) && descriptor.Lifetime == ServiceLifetime.Singleton);

            Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IFeatureFilterMetadata) && descriptor.Lifetime == ServiceLifetime.Scoped);
            Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IFeatureFilterMetadata) && descriptor.Lifetime == ServiceLifetime.Singleton);

            var ex = Assert.Throws<FeatureManagementException>(
                () =>
                {
                    services.AddFeatureManagement();
                });

            Assert.Equal($"Scoped feature management has been registered.", ex.Message);

            services = new ServiceCollection();

            services.AddFeatureManagement();

            ex = Assert.Throws<FeatureManagementException>(
                () =>
                {
                    services.AddScopedFeatureManagement();
                });

            Assert.Equal($"Singleton feature management has been registered.", ex.Message);
        }

        [Fact]
        public async Task LastFeatureFlagWins()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            IServiceCollection services = new ServiceCollection();

            services
                .AddSingleton(configuration)
                .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync(Features.DuplicateFlag));
        }

        [Fact]
        public async Task MergesFeatureFlagsFromDifferentConfigurationSources()
        {
            var mergeOptions = new ConfigurationFeatureDefinitionProviderOptions()
            {
                CustomConfigurationMergingEnabled = true
            };

            /*
             * appsettings1.json
             * Feature1: true
             * Feature2: true
             * FeatureA: true
             * 
             * appsettings2.json
             * Feature1: true
             * Feature2: false
             * FeatureB: true
             * 
             * appsettings3.json
             * Feature1: false
             * Feature2: false
             * FeatureC: true
             */

            IConfiguration configuration1 = new ConfigurationBuilder()
                .AddJsonFile("appsettings1.json")
                .AddJsonFile("appsettings2.json")
                .Build();

            IConfiguration configuration2 = new ConfigurationBuilder()
                .AddConfiguration(configuration1) // chained configuration
                .AddJsonFile("appsettings3.json")
                .Build();

            var featureManager1 = new FeatureManager(new ConfigurationFeatureDefinitionProvider(configuration1, mergeOptions));
            Assert.True(await featureManager1.IsEnabledAsync("FeatureA"));
            Assert.True(await featureManager1.IsEnabledAsync("FeatureB"));
            Assert.True(await featureManager1.IsEnabledAsync("Feature1"));
            Assert.False(await featureManager1.IsEnabledAsync("Feature2")); // appsettings2 should override appsettings1

            var featureManager2 = new FeatureManager(new ConfigurationFeatureDefinitionProvider(configuration2, mergeOptions));
            Assert.True(await featureManager2.IsEnabledAsync("FeatureA"));
            Assert.True(await featureManager2.IsEnabledAsync("FeatureB"));
            Assert.True(await featureManager2.IsEnabledAsync("FeatureC"));
            Assert.False(await featureManager2.IsEnabledAsync("Feature1")); // appsettings3 should override previous settings
            Assert.False(await featureManager2.IsEnabledAsync("Feature2")); // appsettings3 should override previous settings

            //
            // default behavior
            var featureManager3 = new FeatureManager(new ConfigurationFeatureDefinitionProvider(configuration1));
            Assert.False(await featureManager3.IsEnabledAsync("FeatureA")); // it will be overridden by FeatureB
            Assert.True(await featureManager3.IsEnabledAsync("FeatureB"));
            Assert.True(await featureManager3.IsEnabledAsync("Feature1"));
            Assert.False(await featureManager3.IsEnabledAsync("Feature2")); // appsettings2 should override appsettings1

            IConfiguration configuration3 = new ConfigurationBuilder()
                .AddJsonFile("appsettings1.json")
                .AddInMemoryCollection(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["feature_management:feature_flags:0:enabled"] = bool.FalseString,
                    ["feature_management:feature_flags:1:enabled"] = bool.FalseString,
                })
                .Build();
            var featureManager4 = new FeatureManager(new ConfigurationFeatureDefinitionProvider(configuration3));
            Assert.False(await featureManager4.IsEnabledAsync("Feature1"));
            Assert.False(await featureManager4.IsEnabledAsync("Feature2"));
            Assert.True(await featureManager4.IsEnabledAsync("FeatureA"));

            //
            // DI usage
            var services1 = new ServiceCollection();
            services1.Configure<ConfigurationFeatureDefinitionProviderOptions>(o =>
            {
                o.CustomConfigurationMergingEnabled = true;
            });

            services1
                .AddSingleton(configuration2)
                .AddFeatureManagement();
            ServiceProvider serviceProvider1 = services1.BuildServiceProvider();
            IFeatureManager featureManager5 = serviceProvider1.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager5.IsEnabledAsync("FeatureA"));
            Assert.True(await featureManager5.IsEnabledAsync("FeatureB"));
            Assert.True(await featureManager5.IsEnabledAsync("FeatureC"));
            Assert.False(await featureManager5.IsEnabledAsync("Feature1"));
            Assert.False(await featureManager5.IsEnabledAsync("Feature2"));

            var services2 = new ServiceCollection();
            services2.Configure<ConfigurationFeatureDefinitionProviderOptions>(o =>
            {
                o.CustomConfigurationMergingEnabled = false;
            });

            services2
                .AddSingleton(configuration2)
                .AddFeatureManagement();
            ServiceProvider serviceProvider2 = services2.BuildServiceProvider();
            IFeatureManager featureManager6 = serviceProvider2.GetRequiredService<IFeatureManager>();

            Assert.False(await featureManager6.IsEnabledAsync("FeatureA"));
            Assert.False(await featureManager6.IsEnabledAsync("FeatureB"));
            Assert.True(await featureManager6.IsEnabledAsync("FeatureC"));
            Assert.False(await featureManager6.IsEnabledAsync("Feature1"));
            Assert.False(await featureManager6.IsEnabledAsync("Feature2"));
        }
    }

    public class FeatureManagementFeatureFilterGeneralTest
    {
        [Fact]
        public async Task ThrowsExceptionForMissingFeatureFilter()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            FeatureManagementException e = await Assert.ThrowsAsync<FeatureManagementException>(async () => await featureManager.IsEnabledAsync(Features.ConditionalFeature));

            Assert.Equal(FeatureManagementError.MissingFeatureFilter, e.Error);
        }

        [Fact]
        public async Task SwallowsExceptionForMissingFeatureFilter()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .Configure<FeatureManagementOptions>(options =>
                {
                    options.IgnoreMissingFeatureFilters = true;
                });

            services
                .AddSingleton(config)
                .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            var isEnabled = await featureManager.IsEnabledAsync(Features.ConditionalFeature);

            Assert.False(isEnabled);
        }

        [Fact]
        public async Task UsesContext()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<ContextualTestFilter>();

            ServiceProvider provider = serviceCollection.BuildServiceProvider();

            ContextualTestFilter contextualTestFeatureFilter = (ContextualTestFilter)provider.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>().First(f => f is ContextualTestFilter);

            contextualTestFeatureFilter.ContextualCallback = (ctx, accountContext) =>
            {
                var allowedAccounts = new List<string>();

                ctx.Parameters.Bind("AllowedAccounts", allowedAccounts);

                return allowedAccounts.Contains(accountContext.AccountId);
            };

            IFeatureManager featureManager = provider.GetRequiredService<IFeatureManager>();

            var context = new AppContext();

            context.AccountId = "NotEnabledAccount";

            Assert.False(await featureManager.IsEnabledAsync(Features.ContextualFeature, context));

            context.AccountId = "abc";

            Assert.True(await featureManager.IsEnabledAsync(Features.ContextualFeature, context));
        }

        [Fact]
        public async Task UsesRequirementType()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            const string filterOneId = "1";

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<TestFilter>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            string anyFilterFeature = Features.AnyFilterFeature;
            string allFilterFeature = Features.AllFilterFeature;

            IEnumerable<IFeatureFilterMetadata> featureFilters = serviceProvider.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>();

            TestFilter testFeatureFilter = (TestFilter)featureFilters.First(f => f is TestFilter);

            //
            // Set filters to all return true
            testFeatureFilter.Callback = _ => Task.FromResult(true);

            Assert.True(await featureManager.IsEnabledAsync(anyFilterFeature));
            Assert.True(await featureManager.IsEnabledAsync(allFilterFeature));

            //
            // Set filters to all return false
            testFeatureFilter.Callback = ctx => Task.FromResult(false);

            Assert.False(await featureManager.IsEnabledAsync(anyFilterFeature));
            Assert.False(await featureManager.IsEnabledAsync(allFilterFeature));

            //
            // Set 1st filter to true and 2nd filter to false
            testFeatureFilter.Callback = ctx => Task.FromResult(ctx.Parameters["Id"] == filterOneId);

            Assert.True(await featureManager.IsEnabledAsync(anyFilterFeature));
            Assert.False(await featureManager.IsEnabledAsync(allFilterFeature));

            //
            // Set 1st filter to false and 2nd filter to true
            testFeatureFilter.Callback = ctx => Task.FromResult(ctx.Parameters["Id"] != filterOneId);

            Assert.True(await featureManager.IsEnabledAsync(anyFilterFeature));
            Assert.False(await featureManager.IsEnabledAsync(allFilterFeature));
        }

        [Fact]
        public async Task RequirementTypeAllExceptions()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .Configure<FeatureManagementOptions>(options =>
                {
                    options.IgnoreMissingFeatureFilters = true;
                });

            services
                .AddSingleton(config)
                .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            string allFilterFeature = Features.AllFilterFeature;

            await Assert.ThrowsAsync<FeatureManagementException>(async () =>
            {
                await featureManager.IsEnabledAsync(allFilterFeature);
            });
        }

        [Fact]
        public void LimitsFeatureFilterImplementations()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var serviceCollection = new ServiceCollection();

            Assert.Throws<ArgumentException>(() =>
            {
                new ServiceCollection().AddSingleton(config)
                    .AddFeatureManagement()
                    .AddFeatureFilter<InvalidFeatureFilter>();
            });

            Assert.Throws<ArgumentException>(() =>
            {
                new ServiceCollection().AddSingleton(config)
                    .AddFeatureManagement()
                    .AddFeatureFilter<InvalidFeatureFilter2>();
            });
        }

        [Fact]
        public async Task AllowsDuplicatedFilterAlias()
        {
            const string duplicatedFilterName = "DuplicatedFilterName";

            string featureName = Features.FeatureUsesFiltersWithDuplicatedAlias;

            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<DuplicatedAliasFeatureFilter1>()
                .AddFeatureFilter<ContextualDuplicatedAliasFeatureFilterWithAccountContext>()
                .AddFeatureFilter<ContextualDuplicatedAliasFeatureFilterWithDummyContext1>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            var appContext = new AppContext();

            var dummyContext = new DummyContext();

            var targetingContext = new TargetingContext();

            Assert.True(await featureManager.IsEnabledAsync(featureName));

            Assert.True(await featureManager.IsEnabledAsync(featureName, appContext));

            Assert.True(await featureManager.IsEnabledAsync(featureName, dummyContext));

            Assert.True(await featureManager.IsEnabledAsync(featureName, targetingContext));

            services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<DuplicatedAliasFeatureFilter1>();

            serviceProvider = services.BuildServiceProvider();

            featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync(featureName, dummyContext));

            services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<DuplicatedAliasFeatureFilter1>()
                .AddFeatureFilter<DuplicatedAliasFeatureFilter2>();

            serviceProvider = services.BuildServiceProvider();

            featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            var ex = await Assert.ThrowsAsync<FeatureManagementException>(
                async () =>
                {
                    await featureManager.IsEnabledAsync(featureName);
                });

            Assert.Equal($"Multiple feature filters match the configured filter named '{duplicatedFilterName}'.", ex.Message);

            services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<ContextualDuplicatedAliasFeatureFilterWithDummyContext1>()
                .AddFeatureFilter<ContextualDuplicatedAliasFeatureFilterWithDummyContext2>();

            serviceProvider = services.BuildServiceProvider();

            featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            ex = await Assert.ThrowsAsync<FeatureManagementException>(
                async () =>
                {
                    await featureManager.IsEnabledAsync(featureName, dummyContext);
                });

            Assert.Equal($"Multiple contextual feature filters match the configured filter named '{duplicatedFilterName}' and context type '{typeof(DummyContext)}'.", ex.Message);
        }

        [Fact]
        public async Task SkipsContextualFilterEvaluationForUnrecognizedContext()
        {
            string featureName = Features.FeatureUsesFiltersWithDuplicatedAlias;

            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<ContextualDuplicatedAliasFeatureFilterWithAccountContext>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            var dummyContext = new DummyContext();

            Assert.True(await featureManager.IsEnabledAsync(featureName));

            Assert.True(await featureManager.IsEnabledAsync(featureName, dummyContext));
        }

        [Fact]
        public async Task BindsFeatureFlagSettings()
        {
            FeatureFilterConfiguration testFilterConfiguration = new FeatureFilterConfiguration
            {
                Name = "Test",
                Parameters = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "P1", "V1" },
                }).Build()
            };

            var services = new ServiceCollection();

            var definitionProvider = new InMemoryFeatureDefinitionProvider(
                new FeatureDefinition[]
                {
                    new FeatureDefinition
                    {
                        Name = Features.ConditionalFeature,
                        EnabledFor = new List<FeatureFilterConfiguration>()
                        {
                            testFilterConfiguration
                        }
                    }
                });

            services.AddSingleton<IFeatureDefinitionProvider>(definitionProvider)
                    .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                    .AddFeatureManagement()
                    .AddFeatureFilter<TestFilter>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            IEnumerable<IFeatureFilterMetadata> featureFilters = serviceProvider.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>();

            //
            // Sync filter
            TestFilter testFeatureFilter = (TestFilter)featureFilters.First(f => f is TestFilter);

            bool binderCalled = false;

            bool called = false;

            testFeatureFilter.ParametersBinderCallback = (parameters) =>
            {
                binderCalled = true;

                return parameters;
            };

            testFeatureFilter.Callback = (evaluationContext) =>
            {
                called = true;

                return Task.FromResult(true);
            };

            await featureManager.IsEnabledAsync(Features.ConditionalFeature);

            Assert.True(binderCalled);

            Assert.True(called);

            binderCalled = false;

            called = false;

            await featureManager.IsEnabledAsync(Features.ConditionalFeature);

            Assert.False(binderCalled);

            Assert.True(called);

            //
            // Cache break.
            testFilterConfiguration.Parameters = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();

            binderCalled = false;

            called = false;

            await featureManager.IsEnabledAsync(Features.ConditionalFeature);

            Assert.True(binderCalled);

            Assert.True(called);
        }
    }

    public class FeatureManagementBuiltInFeatureFilterTest
    {
        [Fact]
        public async Task TimeWindow()
        {
            const string feature1 = "feature1";
            const string feature2 = "feature2";
            const string feature3 = "feature3";
            const string feature4 = "feature4";
            const string feature5 = "feature5";
            const string feature6 = "feature6";
            const string feature7 = "feature7";

            Environment.SetEnvironmentVariable($"feature_management:feature_flags:0:id", feature1);
            Environment.SetEnvironmentVariable($"feature_management:feature_flags:0:enabled", "true");
            Environment.SetEnvironmentVariable($"feature_management:feature_flags:0:conditions:client_filters:0:name", "TimeWindow");
            Environment.SetEnvironmentVariable($"feature_management:feature_flags:0:conditions:client_filters:0:parameters:End", DateTimeOffset.UtcNow.AddDays(1).ToString("r"));

            Environment.SetEnvironmentVariable($"feature_management:feature_flags:1:id", feature2);
            Environment.SetEnvironmentVariable($"feature_management:feature_flags:1:enabled", "true");
            Environment.SetEnvironmentVariable($"feature_management:feature_flags:1:conditions:client_filters:0:name", "TimeWindow");
            Environment.SetEnvironmentVariable($"feature_management:feature_flags:1:conditions:client_filters:0:parameters:End", DateTimeOffset.UtcNow.ToString("r"));

            Environment.SetEnvironmentVariable($"feature_management:feature_flags:2:id", feature3);
            Environment.SetEnvironmentVariable($"feature_management:feature_flags:2:enabled", "true");
            Environment.SetEnvironmentVariable($"feature_management:feature_flags:2:conditions:client_filters:0:name", "TimeWindow");
            Environment.SetEnvironmentVariable($"feature_management:feature_flags:2:conditions:client_filters:0:parameters:Start", DateTimeOffset.UtcNow.ToString("r"));

            Environment.SetEnvironmentVariable($"feature_management:feature_flags:3:id", feature4);
            Environment.SetEnvironmentVariable($"feature_management:feature_flags:3:enabled", "true");
            Environment.SetEnvironmentVariable($"feature_management:feature_flags:3:conditions:client_filters:0:name", "TimeWindow");
            Environment.SetEnvironmentVariable($"feature_management:feature_flags:3:conditions:client_filters:0:parameters:Start", DateTimeOffset.UtcNow.AddDays(1).ToString("r"));

            Environment.SetEnvironmentVariable("feature_management:feature_flags:4:id", feature5);
            Environment.SetEnvironmentVariable("feature_management:feature_flags:4:enabled", "true");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:4:conditions:client_filters:0:name", "TimeWindow");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:4:conditions:client_filters:0:parameters:Start", DateTimeOffset.UtcNow.AddDays(-2).ToString("r"));
            Environment.SetEnvironmentVariable("feature_management:feature_flags:4:conditions:client_filters:0:parameters:End", DateTimeOffset.UtcNow.AddDays(-1).ToString("r"));
            Environment.SetEnvironmentVariable("feature_management:feature_flags:4:conditions:client_filters:0:parameters:Recurrence:Pattern:Type", "Daily");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:4:conditions:client_filters:0:parameters:Recurrence:Range:Type", "NoEnd");

            Environment.SetEnvironmentVariable("feature_management:feature_flags:5:id", feature6);
            Environment.SetEnvironmentVariable("feature_management:feature_flags:5:enabled", "true");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:5:conditions:client_filters:0:name", "TimeWindow");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:5:conditions:client_filters:0:parameters:Start", DateTimeOffset.UtcNow.AddDays(-2).ToString("r"));
            Environment.SetEnvironmentVariable("feature_management:feature_flags:5:conditions:client_filters:0:parameters:End", DateTimeOffset.UtcNow.AddDays(-1).ToString("r"));
            Environment.SetEnvironmentVariable("feature_management:feature_flags:5:conditions:client_filters:0:parameters:Recurrence:Pattern:Type", "Daily");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:5:conditions:client_filters:0:parameters:Recurrence:Pattern:Interval", "3");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:5:conditions:client_filters:0:parameters:Recurrence:Range:Type", "NoEnd");

            Environment.SetEnvironmentVariable("feature_management:feature_flags:6:id", feature7);
            Environment.SetEnvironmentVariable("feature_management:feature_flags:6:enabled", "true");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:6:conditions:client_filters:0:name", "TimeWindow");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:6:conditions:client_filters:0:parameters:Start", DateTimeOffset.UtcNow.AddDays(-2).ToString("r"));
            Environment.SetEnvironmentVariable("feature_management:feature_flags:6:conditions:client_filters:0:parameters:End", DateTimeOffset.UtcNow.AddDays(-1).ToString("r"));
            Environment.SetEnvironmentVariable("feature_management:feature_flags:6:conditions:client_filters:0:parameters:Recurrence:Pattern:Type", "Weekly");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:6:conditions:client_filters:0:parameters:Recurrence:Pattern:DaysOfWeek:0", "Monday");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:6:conditions:client_filters:0:parameters:Recurrence:Pattern:DaysOfWeek:1", "Tuesday");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:6:conditions:client_filters:0:parameters:Recurrence:Pattern:DaysOfWeek:2", "Wednesday");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:6:conditions:client_filters:0:parameters:Recurrence:Pattern:DaysOfWeek:3", "Thursday");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:6:conditions:client_filters:0:parameters:Recurrence:Pattern:DaysOfWeek:4", "Friday");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:6:conditions:client_filters:0:parameters:Recurrence:Pattern:DaysOfWeek:5", "Saturday");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:6:conditions:client_filters:0:parameters:Recurrence:Pattern:DaysOfWeek:6", "Sunday");
            Environment.SetEnvironmentVariable("feature_management:feature_flags:6:conditions:client_filters:0:parameters:Recurrence:Range:Type", "NoEnd");

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                int dayIndex = (int)day;
                Environment.SetEnvironmentVariable($"feature_management:feature_flags:6:conditions:client_filters:0:parameters:Recurrence:Pattern:DaysOfWeek:{dayIndex}", day.ToString());
            }

            IConfiguration config = new ConfigurationBuilder().AddEnvironmentVariables().Build();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(config)
                .AddFeatureManagement();

            ServiceProvider provider = serviceCollection.BuildServiceProvider();

            IFeatureManager featureManager = provider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync(feature1));
            Assert.False(await featureManager.IsEnabledAsync(feature2));
            Assert.True(await featureManager.IsEnabledAsync(feature3));
            Assert.False(await featureManager.IsEnabledAsync(feature4));
            Assert.True(await featureManager.IsEnabledAsync(feature5));
            Assert.False(await featureManager.IsEnabledAsync(feature6));
            Assert.True(await featureManager.IsEnabledAsync(feature7));
        }

        [Fact]
        public async Task Percentage()
        {
            const string feature = "feature";

            Environment.SetEnvironmentVariable($"feature_management:feature_flags:0:id", feature);
            Environment.SetEnvironmentVariable($"feature_management:feature_flags:0:enabled", "true");
            Environment.SetEnvironmentVariable($"feature_management:feature_flags:0:conditions:client_filters:0:name", "Percentage");
            Environment.SetEnvironmentVariable($"feature_management:feature_flags:0:conditions:client_filters:0:parameters:Value", "50");

            IConfiguration config = new ConfigurationBuilder().AddEnvironmentVariables().Build();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(config)
                .AddFeatureManagement();

            ServiceProvider provider = serviceCollection.BuildServiceProvider();

            IFeatureManager featureManager = provider.GetRequiredService<IFeatureManager>();

            int enabledCount = 0;

            for (int i = 0; i < 10; i++)
            {
                if (await featureManager.IsEnabledAsync(feature))
                {
                    enabledCount++;
                }
            }

            Assert.True(enabledCount >= 0 && enabledCount <= 10);
        }

        [Fact]
        public async Task Targeting()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .Configure<FeatureManagementOptions>(options =>
                {
                    options.IgnoreMissingFeatureFilters = true;
                });

            services
                .AddSingleton(config)
                .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            string targetingTestFeature = Features.TargetingTestFeature;

            //
            // Targeted by user id
            Assert.True(await featureManager.IsEnabledAsync(targetingTestFeature, new TargetingContext
            {
                UserId = "Jeff"
            }));

            //
            // Not targeted by user id, but targeted by default rollout
            Assert.True(await featureManager.IsEnabledAsync(targetingTestFeature, new TargetingContext
            {
                UserId = "Anne"
            }));

            //
            // Not targeted by user id or default rollout
            Assert.False(await featureManager.IsEnabledAsync(targetingTestFeature, new TargetingContext
            {
                UserId = "Patty"
            }));

            //
            // Targeted by group rollout
            Assert.True(await featureManager.IsEnabledAsync(targetingTestFeature, new TargetingContext
            {
                UserId = "Patty",
                Groups = new List<string>() { "Ring1" }
            }));

            //
            // Not targeted by user id, default rollout or group rollout
            Assert.False(await featureManager.IsEnabledAsync(targetingTestFeature, new TargetingContext
            {
                UserId = "Isaac",
                Groups = new List<string>() { "Ring1" }
            }));
        }

        [Fact]
        public async Task TargetingAccessor()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .Configure<FeatureManagementOptions>(options =>
                {
                    options.IgnoreMissingFeatureFilters = true;
                });

            var targetingContextAccessor = new OnDemandTargetingContextAccessor();

            services.AddSingleton<ITargetingContextAccessor>(targetingContextAccessor);

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .WithTargeting<OnDemandTargetingContextAccessor>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            string beta = Features.TargetingTestFeature;

            //
            // Targeted by user id
            targetingContextAccessor.Current = new TargetingContext
            {
                UserId = "Jeff"
            };

            Assert.True(await featureManager.IsEnabledAsync(beta));

            //
            // Not targeted by user id or default rollout
            targetingContextAccessor.Current = new TargetingContext
            {
                UserId = "Patty"
            };

            Assert.False(await featureManager.IsEnabledAsync(beta));

            //
            // Use contextual targeting filter which is registered by default
            Assert.True(await featureManager.IsEnabledAsync(beta, new TargetingContext
            {
                UserId = "Jeff"
            }));
        }

        [Fact]
        public async Task UsesContext()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<ContextualTestFilter>();

            ServiceProvider provider = serviceCollection.BuildServiceProvider();

            ContextualTestFilter contextualTestFeatureFilter = (ContextualTestFilter)provider.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>().First(f => f is ContextualTestFilter);

            contextualTestFeatureFilter.ContextualCallback = (ctx, accountContext) =>
            {
                var allowedAccounts = new List<string>();

                ctx.Parameters.Bind("AllowedAccounts", allowedAccounts);

                return allowedAccounts.Contains(accountContext.AccountId);
            };

            IFeatureManager featureManager = provider.GetRequiredService<IFeatureManager>();

            AppContext context = new AppContext();

            context.AccountId = "NotEnabledAccount";

            Assert.False(await featureManager.IsEnabledAsync(Features.ContextualFeature, context));

            context.AccountId = "abc";

            Assert.True(await featureManager.IsEnabledAsync(Features.ContextualFeature, context));
        }

        [Fact]
        public void LimitsFeatureFilterImplementations()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var serviceCollection = new ServiceCollection();

            Assert.Throws<ArgumentException>(() =>
            {
                new ServiceCollection().AddSingleton(config)
                    .AddFeatureManagement()
                    .AddFeatureFilter<InvalidFeatureFilter>();
            });

            Assert.Throws<ArgumentException>(() =>
            {
                new ServiceCollection().AddSingleton(config)
                    .AddFeatureManagement()
                    .AddFeatureFilter<InvalidFeatureFilter2>();
            });
        }

        [Fact]
        public async Task ListsFeatures()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<ContextualTestFilter>();

            using (ServiceProvider provider = serviceCollection.BuildServiceProvider())
            {
                IFeatureManager featureManager = provider.GetRequiredService<IFeatureManager>();

                bool hasItems = false;

                await foreach (string feature in featureManager.GetFeatureNamesAsync())
                {
                    hasItems = true;

                    break;
                }

                Assert.True(hasItems);
            }
        }

        [Fact]
        public async Task ThrowsExceptionForMissingFeatureFilter()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            FeatureManagementException e = await Assert.ThrowsAsync<FeatureManagementException>(async () => await featureManager.IsEnabledAsync(Features.ConditionalFeature));

            Assert.Equal(FeatureManagementError.MissingFeatureFilter, e.Error);
        }

        [Fact]
        public async Task SwallowsExceptionForMissingFeatureFilter()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .Configure<FeatureManagementOptions>(options =>
                {
                    options.IgnoreMissingFeatureFilters = true;
                });

            services
                .AddSingleton(config)
                .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            var isEnabled = await featureManager.IsEnabledAsync(Features.ConditionalFeature);

            Assert.False(isEnabled);
        }

        [Fact]
        public async Task ThrowsForMissingFeatures()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .Configure<FeatureManagementOptions>(options =>
                {
                    options.IgnoreMissingFeatures = false;
                });

            services
                .AddSingleton(config)
                .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            FeatureManagementException fme = await Assert.ThrowsAsync<FeatureManagementException>(() =>
                featureManager.IsEnabledAsync("NonExistentFeature"));
        }

        [Fact]
        public async Task CustomFeatureDefinitionProvider()
        {
            FeatureDefinition testFeature = new FeatureDefinition
            {
                Name = Features.ConditionalFeature,
                EnabledFor = new List<FeatureFilterConfiguration>()
                {
                    new FeatureFilterConfiguration
                    {
                        Name = "Test",
                        Parameters = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()
                        {
                           { "P1", "V1" },
                        }).Build()
                    }
                }
            };

            var services = new ServiceCollection();

            services.AddSingleton<IFeatureDefinitionProvider>(new InMemoryFeatureDefinitionProvider(new FeatureDefinition[] { testFeature }))
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

        [Fact]
        public async Task ThreadSafeSnapshot()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<TestFilter>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManagerSnapshot>();

            IEnumerable<IFeatureFilterMetadata> featureFilters = serviceProvider.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>();

            //
            // Sync filter
            TestFilter testFeatureFilter = (TestFilter)featureFilters.First(f => f is TestFilter);

            bool called = false;

            testFeatureFilter.Callback = async (evaluationContext) =>
            {
                called = true;

                await Task.Delay(10);

                return new Random().Next(0, 100) > 50;
            };

            var tasks = new List<Task<bool>>();

            for (int i = 0; i < 1000; i++)
            {
                tasks.Add(featureManager.IsEnabledAsync(Features.ConditionalFeature));
            }

            Assert.True(called);

            await Task.WhenAll(tasks);

            bool result = await tasks.First();

            foreach (Task<bool> t in tasks)
            {
                Assert.Equal(result, await t);
            }
        }

        [Fact]
        public async Task TargetingExclusion()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .Configure<FeatureManagementOptions>(options =>
                {
                    options.IgnoreMissingFeatureFilters = true;
                });

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<ContextualTargetingFilter>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            string targetingTestFeature = Features.TargetingTestFeatureWithExclusion;

            //
            // Targeted by user id
            Assert.True(await featureManager.IsEnabledAsync(targetingTestFeature, new TargetingContext
            {
                UserId = "Alicia"
            }));

            //
            // Not targeted by user id, but targeted by default rollout
            Assert.True(await featureManager.IsEnabledAsync(targetingTestFeature, new TargetingContext
            {
                UserId = "Anne"
            }));

            //
            // Not targeted by user id or default rollout
            Assert.False(await featureManager.IsEnabledAsync(targetingTestFeature, new TargetingContext
            {
                UserId = "Patty"
            }));

            //
            // Targeted by group rollout
            Assert.True(await featureManager.IsEnabledAsync(targetingTestFeature, new TargetingContext
            {
                UserId = "Patty",
                Groups = new List<string>() { "Ring1" }
            }));

            //
            // Not targeted by user id, default rollout or group rollout
            Assert.False(await featureManager.IsEnabledAsync(targetingTestFeature, new TargetingContext
            {
                UserId = "Isaac",
                Groups = new List<string>() { "Ring1" }
            }));

            //
            // Excluded by user id
            Assert.False(await featureManager.IsEnabledAsync(targetingTestFeature, new TargetingContext
            {
                UserId = "Jeff"
            }));

            //
            // Excluded by group
            Assert.False(await featureManager.IsEnabledAsync(targetingTestFeature, new TargetingContext
            {
                UserId = "Patty",
                Groups = new List<string>() { "Ring0" }
            }));

            //
            // Included and Excluded by group
            Assert.False(await featureManager.IsEnabledAsync(targetingTestFeature, new TargetingContext
            {
                UserId = "Patty",
                Groups = new List<string>() { "Ring0", "Ring1" }
            }));

            //
            // Included user but Excluded by group
            Assert.False(await featureManager.IsEnabledAsync(targetingTestFeature, new TargetingContext
            {
                UserId = "Alicia",
                Groups = new List<string>() { "Ring2" }
            }));
        }

        [Fact]
        public async Task CustomFilterContextualTargetingWithNullSetting()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            ServiceCollection services = new ServiceCollection();

            var targetingContextAccessor = new OnDemandTargetingContextAccessor();

            services.AddSingleton<ITargetingContextAccessor>(targetingContextAccessor);

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<CustomTargetingFilter>();

            ServiceProvider provider = services.BuildServiceProvider();

            IFeatureManager featureManager = provider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync("CustomFilterFeature"));
        }
    }

    public class FeatureManagementVariantTest
    {
        [Fact]
        public async Task UsesVariants()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

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
            Assert.False(await featureManager.IsEnabledAsync(Features.VariantFeaturePercentileOn, cancellationToken));

            variant = await featureManager.GetVariantAsync(Features.VariantFeaturePercentileOff, cancellationToken);

            Assert.Null(variant);
            Assert.True(await featureManager.IsEnabledAsync(Features.VariantFeaturePercentileOff, cancellationToken));

            // Test Status = Disabled
            variant = await featureManager.GetVariantAsync(Features.VariantFeatureDefaultDisabled, cancellationToken);

            Assert.Equal("Small", variant.Name);
            Assert.Equal("300px", variant.Configuration.Value);
            Assert.False(await featureManager.IsEnabledAsync(Features.VariantFeatureDefaultDisabled, cancellationToken));

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
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

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

        [Fact]
        public async Task VariantBasedInjection()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            IServiceCollection services = new ServiceCollection();

            services.AddSingleton<IAlgorithm, AlgorithmBeta>();
            services.AddSingleton<IAlgorithm, AlgorithmSigma>();
            services.AddSingleton<IAlgorithm>(sp => new AlgorithmOmega("OMEGA"));

            services.AddSingleton(configuration)
                .AddFeatureManagement()
                .AddFeatureFilter<TargetingFilter>()
                .WithVariantService<IAlgorithm>(Features.VariantImplementationFeature);

            var targetingContextAccessor = new OnDemandTargetingContextAccessor();

            services.AddSingleton<ITargetingContextAccessor>(targetingContextAccessor);

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IVariantFeatureManager featureManager = serviceProvider.GetRequiredService<IVariantFeatureManager>();

            IVariantServiceProvider<IAlgorithm> featuredAlgorithm = serviceProvider.GetRequiredService<IVariantServiceProvider<IAlgorithm>>();

            targetingContextAccessor.Current = new TargetingContext
            {
                UserId = "Guest"
            };

            IAlgorithm algorithm = await featuredAlgorithm.GetServiceAsync(CancellationToken.None);

            Assert.Null(algorithm);

            targetingContextAccessor.Current = new TargetingContext
            {
                UserId = "UserSigma"
            };

            algorithm = await featuredAlgorithm.GetServiceAsync(CancellationToken.None);

            Assert.Null(algorithm);

            targetingContextAccessor.Current = new TargetingContext
            {
                UserId = "UserBeta"
            };

            algorithm = await featuredAlgorithm.GetServiceAsync(CancellationToken.None);

            Assert.NotNull(algorithm);
            Assert.Equal("Beta", algorithm.Style);

            targetingContextAccessor.Current = new TargetingContext
            {
                UserId = "UserOmega"
            };

            algorithm = await featuredAlgorithm.GetServiceAsync(CancellationToken.None);

            Assert.NotNull(algorithm);
            Assert.Equal("OMEGA", algorithm.Style);

            services = new ServiceCollection();

            Assert.Throws<InvalidOperationException>(() =>
                {
                    services.AddFeatureManagement()
                        .WithVariantService<IAlgorithm>("DummyFeature1")
                        .WithVariantService<IAlgorithm>("DummyFeature2");
                }
            );
        }

        [Fact]
        public async Task VariantFeatureFlagWithContextualFeatureFilter()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            IServiceCollection services = new ServiceCollection();

            services.AddSingleton(configuration)
                .AddFeatureManagement()
                .AddFeatureFilter<ContextualTestFilter>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            ContextualTestFilter contextualTestFeatureFilter = (ContextualTestFilter)serviceProvider.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>().First(f => f is ContextualTestFilter);

            contextualTestFeatureFilter.ContextualCallback = (ctx, accountContext) =>
            {
                var allowedAccounts = new List<string>();

                ctx.Parameters.Bind("AllowedAccounts", allowedAccounts);

                return allowedAccounts.Contains(accountContext.AccountId);
            };

            IVariantFeatureManager featureManager = serviceProvider.GetRequiredService<IVariantFeatureManager>();

            var context = new AppContext();

            context.AccountId = "NotEnabledAccount";

            Assert.False(await featureManager.IsEnabledAsync(Features.ContextualFeatureWithVariant, context));

            Variant variant = await featureManager.GetVariantAsync(Features.ContextualFeatureWithVariant, context);

            Assert.Equal("Small", variant.Name);

            context.AccountId = "abc";

            Assert.True(await featureManager.IsEnabledAsync(Features.ContextualFeatureWithVariant, context));

            variant = await featureManager.GetVariantAsync(Features.ContextualFeatureWithVariant, context);

            Assert.Equal("Big", variant.Name);
        }
    }

    public class FeatureManagementTelemetryTest
    {
        [Fact]
        public async Task TelemetryPublishing()
        {
            int currentTest = 0;

            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            var targetingContextAccessor = new OnDemandTargetingContextAccessor();
            services.AddSingleton<ITargetingContextAccessor>(targetingContextAccessor)
                .AddSingleton(config)
                .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            FeatureManager featureManager = (FeatureManager)serviceProvider.GetRequiredService<IVariantFeatureManager>();
            CancellationToken cancellationToken = CancellationToken.None;

            using Activity testActivity = new Activity("TestActivity").Start();

            // Start listener
            using ActivityListener activityListener = new ActivityListener
            {
                ShouldListenTo = (activitySource) => activitySource.Name == "Microsoft.FeatureManagement",
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
                ActivityStopped = (activity) =>
                {
                    // Stop other tests from asserting
                    if (activity.ParentId != testActivity.Id)
                    {
                        return;
                    }

                    ActivityEvent? evaluationEventNullable = activity.Events.FirstOrDefault((activityEvent) => activityEvent.Name == "FeatureFlag");

                    if (evaluationEventNullable != null && evaluationEventNullable.Value.Tags.Any())
                    {
                        ActivityEvent evaluationEvent = evaluationEventNullable.Value;

                        string featureName = evaluationEvent.Tags.FirstOrDefault(kvp => kvp.Key == "FeatureName").Value?.ToString();
                        string targetingId = evaluationEvent.Tags.FirstOrDefault(kvp => kvp.Key == "TargetingId").Value?.ToString();
                        string variantName = evaluationEvent.Tags.FirstOrDefault(kvp => kvp.Key == "Variant").Value?.ToString();
                        string enabled = evaluationEvent.Tags.FirstOrDefault(kvp => kvp.Key == "Enabled").Value?.ToString();
                        string variantAssignmentReason = evaluationEvent.Tags.FirstOrDefault(kvp => kvp.Key == "VariantAssignmentReason").Value?.ToString();
                        string version = evaluationEvent.Tags.FirstOrDefault(kvp => kvp.Key == "Version").Value?.ToString();
                        string etag = evaluationEvent.Tags.FirstOrDefault(kvp => kvp.Key == "Etag").Value?.ToString();
                        string label = evaluationEvent.Tags.FirstOrDefault(kvp => kvp.Key == "Label").Value?.ToString();
                        string firstTag = evaluationEvent.Tags.FirstOrDefault(kvp => kvp.Key == "Tags.Tag1").Value?.ToString();

                        string variantAssignmentPercentage = evaluationEvent.Tags.FirstOrDefault(kvp => kvp.Key == "VariantAssignmentPercentage").Value?.ToString();
                        string defaultWhenEnabled = evaluationEvent.Tags.FirstOrDefault(kvp => kvp.Key == "DefaultWhenEnabled").Value?.ToString();

                        // Test telemetry cases
                        switch (featureName)
                        {
                            case Features.OnTelemetryTestFeature:
                                Assert.Equal(1, currentTest);
                                currentTest = 0;
                                Assert.Equal("True", enabled);
                                Assert.Equal("EtagValue", etag);
                                Assert.Equal("LabelValue", label);
                                Assert.Equal("Tag1Value", firstTag);
                                Assert.Null(variantName);
                                Assert.Equal(VariantAssignmentReason.None.ToString(), variantAssignmentReason);
                                break;

                            case Features.OffTelemetryTestFeature:
                                Assert.Equal(2, currentTest);
                                currentTest = 0;
                                Assert.Equal("False", enabled);
                                Assert.Equal(VariantAssignmentReason.None.ToString(), variantAssignmentReason);
                                break;

                            case Features.VariantFeatureDefaultEnabled:
                                Assert.Equal(3, currentTest);
                                currentTest = 0;
                                Assert.Equal("True", enabled);
                                Assert.Equal("Medium", variantName);
                                Assert.Equal(VariantAssignmentReason.DefaultWhenEnabled.ToString(), variantAssignmentReason);
                                Assert.Equal("100", variantAssignmentPercentage);
                                Assert.Equal("Medium", defaultWhenEnabled);
                                break;

                            case Features.VariantFeatureDefaultDisabled:
                                Assert.Equal(4, currentTest);
                                currentTest = 0;
                                Assert.Equal("False", enabled);
                                Assert.Equal("Small", variantName);
                                Assert.Equal(VariantAssignmentReason.DefaultWhenDisabled.ToString(), variantAssignmentReason);
                                Assert.Null(variantAssignmentPercentage);
                                Assert.Null(defaultWhenEnabled);
                                break;

                            case Features.VariantFeaturePercentileOn:
                                Assert.Equal(5, currentTest);
                                currentTest = 0;
                                Assert.Equal("Big", variantName);
                                Assert.Equal("Marsha", targetingId);
                                Assert.Equal(VariantAssignmentReason.Percentile.ToString(), variantAssignmentReason);
                                break;

                            case Features.VariantFeaturePercentileOff:
                                Assert.Equal(6, currentTest);
                                currentTest = 0;
                                Assert.Null(variantName);
                                Assert.Equal(VariantAssignmentReason.DefaultWhenEnabled.ToString(), variantAssignmentReason);
                                break;

                            case Features.VariantFeatureAlwaysOff:
                                Assert.Equal(7, currentTest);
                                currentTest = 0;
                                Assert.Null(variantName);
                                Assert.Equal(VariantAssignmentReason.DefaultWhenDisabled.ToString(), variantAssignmentReason);
                                Assert.Null(variantAssignmentPercentage);
                                Assert.Null(defaultWhenEnabled);
                                break;

                            case Features.VariantFeatureUser:
                                Assert.Equal(8, currentTest);
                                currentTest = 0;
                                Assert.Equal("Small", variantName);
                                Assert.Equal(VariantAssignmentReason.User.ToString(), variantAssignmentReason);
                                Assert.Null(variantAssignmentPercentage);
                                Assert.Null(defaultWhenEnabled);
                                break;

                            case Features.VariantFeatureGroup:
                                Assert.Equal(9, currentTest);
                                currentTest = 0;
                                Assert.Equal("Small", variantName);
                                Assert.Equal(VariantAssignmentReason.Group.ToString(), variantAssignmentReason);
                                Assert.Null(variantAssignmentPercentage);
                                Assert.Null(defaultWhenEnabled);
                                break;

                            case Features.VariantFeatureNoVariants:
                                Assert.Equal(10, currentTest);
                                currentTest = 0;
                                Assert.Null(variantName);
                                Assert.Equal(VariantAssignmentReason.None.ToString(), variantAssignmentReason);
                                Assert.Null(variantAssignmentPercentage);
                                Assert.Null(defaultWhenEnabled);
                                break;

                            case Features.VariantFeatureNoAllocation:
                                Assert.Equal(11, currentTest);
                                currentTest = 0;
                                Assert.Null(variantName);
                                Assert.Equal(VariantAssignmentReason.DefaultWhenEnabled.ToString(), variantAssignmentReason);
                                Assert.Equal("100", variantAssignmentPercentage);
                                Assert.Null(defaultWhenEnabled);
                                break;

                            case Features.VariantFeatureAlwaysOffNoAllocation:
                                Assert.Equal(12, currentTest);
                                currentTest = 0;
                                Assert.Null(variantName);
                                Assert.Equal(VariantAssignmentReason.DefaultWhenDisabled.ToString(), variantAssignmentReason);
                                Assert.Null(variantAssignmentPercentage);
                                Assert.Null(defaultWhenEnabled);
                                break;

                            case Features.VariantFeatureIncorrectDefaultWhenEnabled:
                                Assert.Equal(13, currentTest);
                                currentTest = 0;
                                Assert.Null(variantName);
                                Assert.Equal(VariantAssignmentReason.DefaultWhenEnabled.ToString(), variantAssignmentReason);
                                Assert.Equal("100", variantAssignmentPercentage);
                                Assert.Equal("Foo", defaultWhenEnabled);
                                break;

                            default:
                                throw new Exception("Unexpected feature name");
                        }
                    }
                }
            };
            ActivitySource.AddActivityListener(activityListener);

            currentTest = 1;
            await featureManager.IsEnabledAsync(Features.OnTelemetryTestFeature, cancellationToken);
            Assert.Equal(0, currentTest);

            currentTest = 2;
            await featureManager.IsEnabledAsync(Features.OffTelemetryTestFeature, cancellationToken);
            Assert.Equal(0, currentTest);

            // Test variant cases
            currentTest = 3;
            await featureManager.IsEnabledAsync(Features.VariantFeatureDefaultEnabled, cancellationToken);
            Assert.Equal(0, currentTest);

            currentTest = 4;
            await featureManager.IsEnabledAsync(Features.VariantFeatureDefaultDisabled, cancellationToken);
            Assert.Equal(0, currentTest);

            targetingContextAccessor.Current = new TargetingContext
            {
                UserId = "Marsha",
                Groups = new List<string> { "Group1" }
            };
            currentTest = 5;
            await featureManager.GetVariantAsync(Features.VariantFeaturePercentileOn, cancellationToken);
            Assert.Equal(0, currentTest);

            currentTest = 6;
            await featureManager.GetVariantAsync(Features.VariantFeaturePercentileOff, cancellationToken);
            Assert.Equal(0, currentTest);

            currentTest = 7;
            await featureManager.GetVariantAsync(Features.VariantFeatureAlwaysOff, cancellationToken);
            Assert.Equal(0, currentTest);

            currentTest = 8;
            await featureManager.GetVariantAsync(Features.VariantFeatureUser, cancellationToken);
            Assert.Equal(0, currentTest);

            currentTest = 9;
            await featureManager.GetVariantAsync(Features.VariantFeatureGroup, cancellationToken);
            Assert.Equal(0, currentTest);

            currentTest = 10;
            await featureManager.GetVariantAsync(Features.VariantFeatureNoVariants, cancellationToken);
            Assert.Equal(0, currentTest);

            currentTest = 11;
            await featureManager.GetVariantAsync(Features.VariantFeatureNoAllocation, cancellationToken);
            Assert.Equal(0, currentTest);

            currentTest = 12;
            await featureManager.GetVariantAsync(Features.VariantFeatureAlwaysOffNoAllocation, cancellationToken);
            Assert.Equal(0, currentTest);

            currentTest = 13;
            await featureManager.GetVariantAsync(Features.VariantFeatureIncorrectDefaultWhenEnabled, cancellationToken);
            Assert.Equal(0, currentTest);

            // Test a feature with telemetry disabled- should throw if the listener hits it
            bool result = await featureManager.IsEnabledAsync(Features.OnTestFeature, cancellationToken);

            Assert.True(result);
        }
    }

    public class CustomImplementationsFeatureManagementTests
    {
        public class CustomIFeatureManager : IFeatureManager
        {
            public IAsyncEnumerable<string> GetFeatureNamesAsync()
            {
                return new string[1] { "Test" }.ToAsyncEnumerable();
            }

            public async Task<bool> IsEnabledAsync(string feature)
            {
                return await Task.FromResult(feature == "Test");
            }

            public async Task<bool> IsEnabledAsync<TContext>(string feature, TContext context)
            {
                return await Task.FromResult(feature == "Test");
            }
        }

        [Fact]
        public async Task CustomIFeatureManagerTest()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services.AddSingleton(config)
                    .AddSingleton<IFeatureManager, CustomIFeatureManager>()
                    .AddFeatureManagement(); // Shouldn't override

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync("Test"));
            Assert.False(await featureManager.IsEnabledAsync("NotTest"));

            // Provider shouldn't be affected
            IFeatureDefinitionProvider featureDefinitionProvider = serviceProvider.GetRequiredService<IFeatureDefinitionProvider>();

            Assert.True(await featureDefinitionProvider.GetAllFeatureDefinitionsAsync().AnyAsync());
            Assert.NotNull(await featureDefinitionProvider.GetFeatureDefinitionAsync("OnTestFeature"));

            // Snapshot should use available IFeatureManager
            FeatureManagerSnapshot featureManagerSnapshot = serviceProvider.GetRequiredService<FeatureManagerSnapshot>();

            Assert.True(await featureManagerSnapshot.IsEnabledAsync("Test"));
            Assert.False(await featureManagerSnapshot.IsEnabledAsync("NotTest"));
            Assert.False(await featureManagerSnapshot.IsEnabledAsync("OnTestFeature"));

            // Use snapshot results even though IVariantFeatureManager would be called here
            Assert.True(await featureManagerSnapshot.IsEnabledAsync("Test", CancellationToken.None));
            Assert.False(await featureManagerSnapshot.IsEnabledAsync("NotTest", CancellationToken.None));
            Assert.False(await featureManagerSnapshot.IsEnabledAsync("OnTestFeature", CancellationToken.None));
        }

        [Fact]
        public async Task CustomIFeatureDefinitionProvider()
        {
            FeatureDefinition testFeature = new FeatureDefinition
            {
                Name = Features.ConditionalFeature,
                EnabledFor = new List<FeatureFilterConfiguration>()
                {
                    new FeatureFilterConfiguration
                    {
                        Name = "Test",
                        Parameters = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()
                        {
                           { "P1", "V1" },
                        }).Build()
                    }
                }
            };

            var services = new ServiceCollection();

            services.AddSingleton<IFeatureDefinitionProvider>(new InMemoryFeatureDefinitionProvider(new FeatureDefinition[] { testFeature }))
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

    public class PerformanceTests
    {
        [Fact]
        public async Task BooleanFlagManyTimes()
        {
            var services = new ServiceCollection();

            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            services.AddSingleton(config)
                .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IVariantFeatureManager featureManager = serviceProvider.GetRequiredService<IVariantFeatureManager>();

            bool result;

            for (int i = 0; i < 100000; i++)
            {
                result = await featureManager.IsEnabledAsync("OnTestFeature");
            }
        }

        [Fact]
        public async Task MissingFlagManyTimes()
        {
            var services = new ServiceCollection();

            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            services.AddSingleton(config)
                .AddFeatureManagement();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IVariantFeatureManager featureManager = serviceProvider.GetRequiredService<IVariantFeatureManager>();

            bool result;

            for (int i = 0; i < 100000; i++)
            {
                result = await featureManager.IsEnabledAsync("DoesNotExist");
            }
        }
    }
}
