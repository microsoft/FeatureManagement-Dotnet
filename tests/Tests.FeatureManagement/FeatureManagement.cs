// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.FeatureManagement
{
    public class FeatureManagement
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
        public async Task ReadsTopLevelConfiguration()
        {
            const string feature = "FeatureX";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes($"{{\"AllowedHosts\": \"*\", \"FeatureFlags\": {{\"{feature}\": true}}}}"));

            IConfiguration config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            var services = new ServiceCollection();

            services.AddFeatureManagement(config.GetSection("FeatureFlags"));

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync(feature));
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

        [Fact]
        public async Task TimeWindow()
        {
            const string feature1 = "feature1";
            const string feature2 = "feature2";
            const string feature3 = "feature3";
            const string feature4 = "feature4";

            Environment.SetEnvironmentVariable($"FeatureManagement:{feature1}:EnabledFor:0:Name", "TimeWindow");
            Environment.SetEnvironmentVariable($"FeatureManagement:{feature1}:EnabledFor:0:Parameters:End", DateTimeOffset.UtcNow.AddDays(1).ToString("r"));

            Environment.SetEnvironmentVariable($"FeatureManagement:{feature2}:EnabledFor:0:Name", "TimeWindow");
            Environment.SetEnvironmentVariable($"FeatureManagement:{feature2}:EnabledFor:0:Parameters:End", DateTimeOffset.UtcNow.ToString("r"));

            Environment.SetEnvironmentVariable($"FeatureManagement:{feature3}:EnabledFor:0:Name", "TimeWindow");
            Environment.SetEnvironmentVariable($"FeatureManagement:{feature3}:EnabledFor:0:Parameters:Start", DateTimeOffset.UtcNow.ToString("r"));

            Environment.SetEnvironmentVariable($"FeatureManagement:{feature4}:EnabledFor:0:Name", "TimeWindow");
            Environment.SetEnvironmentVariable($"FeatureManagement:{feature4}:EnabledFor:0:Parameters:Start", DateTimeOffset.UtcNow.AddDays(1).ToString("r"));

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
        }

        [Fact]
        public async Task Percentage()
        {
            const string feature = "feature";

            Environment.SetEnvironmentVariable($"FeatureManagement:{feature}:EnabledFor:0:Name", "Percentage");
            Environment.SetEnvironmentVariable($"FeatureManagement:{feature}:EnabledFor:0:Parameters:Value", "50");

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

            Assert.True(enabledCount >= 0 && enabledCount < 10);
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
        public async Task ThreadsafeSnapshot()
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

            bool result = tasks.First().Result;

            foreach (Task<bool> t in tasks)
            {
                Assert.Equal(result, t.Result);
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

        [Fact]
        public async Task TelemetryPublishing()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddTelemetryPublisher<TestTelemetryPublisher>()
                .AddFeatureFilter<TimeWindowFilter>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            FeatureManager featureManager = (FeatureManager) serviceProvider.GetRequiredService<IVariantFeatureManager>();
            TestTelemetryPublisher testPublisher = (TestTelemetryPublisher) featureManager.TelemetryPublishers.First();

            // Test a feature with telemetry disabled
            bool result = await featureManager.IsEnabledAsync(Features.OnTestFeature, CancellationToken.None);

            Assert.True(result);
            Assert.Null(testPublisher.evaluationEventCache);

            // Test telemetry cases
            const string onFeature = "AlwaysOnTestFeature";

            result = await featureManager.IsEnabledAsync(onFeature, CancellationToken.None);

            Assert.True(result);
            Assert.Equal(onFeature, testPublisher.evaluationEventCache.FeatureDefinition.Name);
            Assert.Equal(result, testPublisher.evaluationEventCache.IsEnabled);
            Assert.Equal("EtagValue", testPublisher.evaluationEventCache.FeatureDefinition.TelemetryMetadata["Etag"]);
            Assert.Equal("LabelValue", testPublisher.evaluationEventCache.FeatureDefinition.TelemetryMetadata["Label"]);
            Assert.Equal("Tag1Value", testPublisher.evaluationEventCache.FeatureDefinition.TelemetryMetadata["Tags.Tag1"]);

            const string offFeature = "OffTimeTestFeature";

            result = await featureManager.IsEnabledAsync(offFeature, CancellationToken.None);

            Assert.False(result);
            Assert.Equal(offFeature, testPublisher.evaluationEventCache.FeatureDefinition.Name);
            Assert.Equal(result, testPublisher.evaluationEventCache.IsEnabled);

            // Test variant cases
            const string variantDefaultEnabledFeature = "VariantFeatureDefaultEnabled";

            result = await featureManager.IsEnabledAsync(variantDefaultEnabledFeature, CancellationToken.None);

            Assert.True(result);
            Assert.Equal(variantDefaultEnabledFeature, testPublisher.evaluationEventCache.FeatureDefinition.Name);
            Assert.Equal(result, testPublisher.evaluationEventCache.IsEnabled);
            Assert.Equal("Medium", testPublisher.evaluationEventCache.Variant.Name);

            Variant variantResult = await featureManager.GetVariantAsync(variantDefaultEnabledFeature, CancellationToken.None);

            Assert.True(testPublisher.evaluationEventCache.IsEnabled);
            Assert.Equal(variantDefaultEnabledFeature, testPublisher.evaluationEventCache.FeatureDefinition.Name);
            Assert.Equal(variantResult.Name, testPublisher.evaluationEventCache.Variant.Name);

            string variantFeatureStatusDisabled = "VariantFeatureStatusDisabled";

            result = await featureManager.IsEnabledAsync(variantFeatureStatusDisabled, CancellationToken.None);

            Assert.False(result);
            Assert.Equal(variantFeatureStatusDisabled, testPublisher.evaluationEventCache.FeatureDefinition.Name);
            Assert.Equal(result, testPublisher.evaluationEventCache.IsEnabled);
            Assert.Equal("Small", testPublisher.evaluationEventCache.Variant.Name);

            variantResult = await featureManager.GetVariantAsync(variantFeatureStatusDisabled, CancellationToken.None);

            Assert.False(testPublisher.evaluationEventCache.IsEnabled);
            Assert.Equal(variantFeatureStatusDisabled, testPublisher.evaluationEventCache.FeatureDefinition.Name);
            Assert.Equal(variantResult.Name, testPublisher.evaluationEventCache.Variant.Name);
        }

        [Fact]
        public async Task TelemetryPublishingNullPublisher()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<TimeWindowFilter>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            FeatureManager featureManager = (FeatureManager)serviceProvider.GetRequiredService<IVariantFeatureManager>();

            // Test telemetry enabled feature with no telemetry publisher
            string onFeature = "AlwaysOnTestFeature";

            bool result = await featureManager.IsEnabledAsync(onFeature, CancellationToken.None);

            Assert.True(result);
        }

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
            Variant variant = await featureManager.GetVariantAsync("VariantFeaturePercentileOn", cancellationToken);

            Assert.Equal("Big", variant.Name);
            Assert.Equal("green", variant.Configuration["Color"]);
            Assert.False(await featureManager.IsEnabledAsync("VariantFeaturePercentileOn", cancellationToken));

            variant = await featureManager.GetVariantAsync("VariantFeaturePercentileOff", cancellationToken);

            Assert.Null(variant);
            Assert.True(await featureManager.IsEnabledAsync("VariantFeaturePercentileOff", cancellationToken));

            // Test Status = Disabled
            variant = await featureManager.GetVariantAsync("VariantFeatureStatusDisabled", cancellationToken);

            Assert.Equal("Small", variant.Name);
            Assert.Equal("300px", variant.Configuration.Value);
            Assert.False(await featureManager.IsEnabledAsync("VariantFeatureStatusDisabled", cancellationToken));

            // Test DefaultWhenEnabled and ConfigurationValue with inline IConfigurationSection
            variant = await featureManager.GetVariantAsync("VariantFeatureDefaultEnabled", cancellationToken);

            Assert.Equal("Medium", variant.Name);
            Assert.Equal("450px", variant.Configuration["Size"]);
            Assert.True(await featureManager.IsEnabledAsync("VariantFeatureDefaultEnabled", cancellationToken));

            // Test User allocation
            variant = await featureManager.GetVariantAsync("VariantFeatureUser", cancellationToken);

            Assert.Equal("Small", variant.Name);
            Assert.Equal("300px", variant.Configuration.Value);
            Assert.True(await featureManager.IsEnabledAsync("VariantFeatureUser", cancellationToken));

            // Test Group allocation
            variant = await featureManager.GetVariantAsync("VariantFeatureGroup", cancellationToken);

            Assert.Equal("Small", variant.Name);
            Assert.Equal("300px", variant.Configuration.Value);
            Assert.True(await featureManager.IsEnabledAsync("VariantFeatureGroup", cancellationToken));
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
            Variant variant = await featureManager.GetVariantAsync("VariantFeatureNoVariants", cancellationToken);

            Assert.Null(variant);

            // Verify null variant returned if no allocation is specified
            variant = await featureManager.GetVariantAsync("VariantFeatureNoAllocation", cancellationToken);

            Assert.Null(variant);

            // Verify that ConfigurationValue has priority over ConfigurationReference
            variant = await featureManager.GetVariantAsync("VariantFeatureBothConfigurations", cancellationToken);

            Assert.Equal("600px", variant.Configuration.Value);

            // Verify that an exception is thrown for invalid StatusOverride value
            FeatureManagementException e = await Assert.ThrowsAsync<FeatureManagementException>(async () =>
            {
                variant = await featureManager.GetVariantAsync("VariantFeatureInvalidStatusOverride", cancellationToken);
            });

            Assert.Equal(FeatureManagementError.InvalidConfigurationSetting, e.Error);
            Assert.Contains(ConfigurationFields.VariantDefinitionStatusOverride, e.Message);

            // Verify that an exception is thrown for invalid doubles From and To in the Percentile section
            e = await Assert.ThrowsAsync<FeatureManagementException>(async () =>
            {
                variant = await featureManager.GetVariantAsync("VariantFeatureInvalidFromTo", cancellationToken);
            });

            Assert.Equal(FeatureManagementError.InvalidConfigurationSetting, e.Error);
            Assert.Contains(ConfigurationFields.PercentileAllocationFrom, e.Message);
        }
    }
}
