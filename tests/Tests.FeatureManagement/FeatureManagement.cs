// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

            Assert.True(await featureManager.IsEnabledAsync(Enum.GetName(typeof(Features), Features.OnTestFeature)));

            Assert.False(await featureManager.IsEnabledAsync(Enum.GetName(typeof(Features), Features.OffTestFeature)));

            IEnumerable<IFeatureFilterMetadata> featureFilters = serviceProvider.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>();

            //
            // Sync filter
            TestFilter testFeatureFilter = (TestFilter)featureFilters.First(f => f is TestFilter);

            bool called = false;

            testFeatureFilter.Callback = (evaluationContext) =>
            {
                called = true;

                Assert.Equal("V1", evaluationContext.Parameters["P1"]);

                Assert.Equal(Enum.GetName(typeof(Features), Features.ConditionalFeature), evaluationContext.FeatureName);

                return Task.FromResult(true);
            };

            await featureManager.IsEnabledAsync(Enum.GetName(typeof(Features), Features.ConditionalFeature));

            Assert.True(called);
        }

        [Fact]
        public async Task ReadsOnlyFeatureManagementSection()
        {
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"AllowedHosts\": \"*\"}"));
            IConfiguration config = new ConfigurationBuilder().AddJsonStream(stream).Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<TestFilter>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            await foreach (string featureName in featureManager.GetFeatureNamesAsync())
            {
                // Fail, as no features should be found
                Assert.True(false);
            }
        }

        [Fact]
        public async Task AllowsDuplicatedFilterAlias()
        {
            const string duplicatedFilterName = "DuplicatedFilterName";

            string featureName = Enum.GetName(typeof(Features), Features.FeatureUsesFiltersWithDuplicatedAlias);

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

            services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<ContextualDuplicatedAliasFeatureFilterWithAccountContext>();

            serviceProvider = services.BuildServiceProvider();

            featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            ex = await Assert.ThrowsAsync<FeatureManagementException>(
                async () =>
                {
                    await featureManager.IsEnabledAsync(featureName);
                });

            Assert.Equal($"The feature filter '{duplicatedFilterName}' specified for feature '{featureName}' was not found.", ex.Message);
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
            string feature1 = "feature1";
            string feature2 = "feature2";
            string feature3 = "feature3";
            string feature4 = "feature4";

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
            string feature1 = "feature1";

            Environment.SetEnvironmentVariable($"FeatureManagement:{feature1}:EnabledFor:0:Name", "Percentage");
            Environment.SetEnvironmentVariable($"FeatureManagement:{feature1}:EnabledFor:0:Parameters:Value", "50");

            IConfiguration config = new ConfigurationBuilder().AddEnvironmentVariables().Build();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(config)
                .AddFeatureManagement();

            ServiceProvider provider = serviceCollection.BuildServiceProvider();

            IFeatureManager featureManager = provider.GetRequiredService<IFeatureManager>();

            int enabledCount = 0;

            for (int i = 0; i < 10; i++)
            {
                if (await featureManager.IsEnabledAsync(feature1))
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

            string targetingTestFeature = Enum.GetName(typeof(Features), Features.TargetingTestFeature);

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

            string beta = Enum.GetName(typeof(Features), Features.TargetingTestFeature);

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

            Assert.False(await featureManager.IsEnabledAsync(Enum.GetName(typeof(Features), Features.ContextualFeature), context));

            context.AccountId = "abc";

            Assert.True(await featureManager.IsEnabledAsync(Enum.GetName(typeof(Features), Features.ContextualFeature), context));
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

            FeatureManagementException e = await Assert.ThrowsAsync<FeatureManagementException>(async () => await featureManager.IsEnabledAsync(Enum.GetName(typeof(Features), Features.ConditionalFeature)));

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

            var isEnabled = await featureManager.IsEnabledAsync(Enum.GetName(typeof(Features), Features.ConditionalFeature));

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
                Name = Enum.GetName(typeof(Features), Features.ConditionalFeature),
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

                Assert.Equal(Enum.GetName(typeof(Features), Features.ConditionalFeature), evaluationContext.FeatureName);

                return Task.FromResult(true);
            };

            await featureManager.IsEnabledAsync(Enum.GetName(typeof(Features), Features.ConditionalFeature));

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
                tasks.Add(featureManager.IsEnabledAsync(Enum.GetName(typeof(Features), Features.ConditionalFeature)));
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

            string targetingTestFeature = Enum.GetName(typeof(Features), Features.TargetingTestFeatureWithExclusion);

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

            string filterOneId = "1";

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<TestFilter>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            string anyFilterFeature = Enum.GetName(typeof(Features), Features.AnyFilterFeature);
            string allFilterFeature = Enum.GetName(typeof(Features), Features.AllFilterFeature);

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

            string allFilterFeature = Enum.GetName(typeof(Features), Features.AllFilterFeature);

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
                        Name = Enum.GetName(typeof(Features), Features.ConditionalFeature),
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

            await featureManager.IsEnabledAsync(Enum.GetName(typeof(Features), Features.ConditionalFeature));

            Assert.True(binderCalled);

            Assert.True(called);

            binderCalled = false;

            called = false;

            await featureManager.IsEnabledAsync(Enum.GetName(typeof(Features), Features.ConditionalFeature));

            Assert.False(binderCalled);

            Assert.True(called);

            //
            // Cache break.
            testFilterConfiguration.Parameters = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();

            binderCalled = false;

            called = false;

            await featureManager.IsEnabledAsync(Enum.GetName(typeof(Features), Features.ConditionalFeature));

            Assert.True(binderCalled);

            Assert.True(called);
        }
    }
}
