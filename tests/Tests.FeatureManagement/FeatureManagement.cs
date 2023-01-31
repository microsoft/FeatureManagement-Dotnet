// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Assigners;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.FeatureManagement
{
    public class FeatureManagement
    {
        private const string OnFeature = "OnTestFeature";
        private const string OffFeature = "OffFeature";
        private const string ConditionalFeature = "ConditionalFeature";
        private const string ContextualFeature = "ContextualFeature";
        private const string WithSuffixFeature = "WithSuffixFeature";
        private const string WithoutSuffixFeature = "WithoutSuffixFeature";

        [Fact]
        public async Task ReadsConfiguration()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<TestFilter>()
                .AddFeatureVariantAssigner<TestAssigner>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync(OnFeature));

            Assert.False(await featureManager.IsEnabledAsync(OffFeature));

            IEnumerable<IFeatureFilterMetadata> featureFilters = serviceProvider.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>();

            //
            // Sync filter
            TestFilter testFeatureFilter = (TestFilter)featureFilters.First(f => f is TestFilter);

            bool called = false;

            testFeatureFilter.Callback = (evaluationContext) =>
            {
                called = true;

                Assert.Equal("V1", evaluationContext.Parameters["P1"]);

                Assert.Equal(ConditionalFeature, evaluationContext.FeatureFlagName);

                return Task.FromResult(true);
            };

            await featureManager.IsEnabledAsync(ConditionalFeature);

            Assert.True(called);

            IDynamicFeatureManager variantManager = serviceProvider.GetRequiredService<IDynamicFeatureManager>();

            IEnumerable<IFeatureVariantAssignerMetadata> featureVariantAssigners = serviceProvider.GetRequiredService<IEnumerable<IFeatureVariantAssignerMetadata>>();

            TestAssigner testAssigner = (TestAssigner)featureVariantAssigners.First(f => f is TestAssigner);

            called = false;

            testAssigner.Callback = (evaluationContext) =>
            {
                called = true;

                Assert.Equal(2, evaluationContext.FeatureDefinition.Variants.Count());

                Assert.Equal(Features.VariantFeature, evaluationContext.FeatureDefinition.Name);

                FeatureVariant defaultVariant = evaluationContext.FeatureDefinition.Variants.First(v => v.Default);

                FeatureVariant otherVariant = evaluationContext.FeatureDefinition.Variants.First(v => !v.Default);

                //
                // default variant
                Assert.Equal("V1", defaultVariant.Name);

                Assert.Equal("Ref1", defaultVariant.ConfigurationReference);

                // other variant
                Assert.Equal("V2", otherVariant.Name);

                Assert.Equal("Ref2", otherVariant.ConfigurationReference);

                Assert.Equal("V1", otherVariant.AssignmentParameters["P1"]);

                return otherVariant;
            };

            string val = await variantManager.GetVariantAsync<string>(Features.VariantFeature, CancellationToken.None);

            Assert.True(called);

            Assert.Equal("def", val);
        }

        [Fact]
        public async Task ReadsV1Configuration()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.v1.json").Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<TestFilter>()
                .AddFeatureVariantAssigner<TestAssigner>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

            Assert.True(await featureManager.IsEnabledAsync(OnFeature, CancellationToken.None));

            Assert.False(await featureManager.IsEnabledAsync(OffFeature, CancellationToken.None));

            IEnumerable<IFeatureFilterMetadata> featureFilters = serviceProvider.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>();

            //
            // Sync filter
            TestFilter testFeatureFilter = (TestFilter)featureFilters.First(f => f is TestFilter);

            bool called = false;

            testFeatureFilter.Callback = (evaluationContext) =>
            {
                called = true;

                Assert.Equal("V1", evaluationContext.Parameters["P1"]);

                Assert.Equal(ConditionalFeature, evaluationContext.FeatureFlagName);

                return Task.FromResult(true);
            };

            await featureManager.IsEnabledAsync(ConditionalFeature, CancellationToken.None);

            Assert.True(called);
        }

        [Fact]
        public async Task AllowsSuffix()
        {
            /*
             * Verifies a filter named ___Filter can be referenced with "___" or "___Filter"
             */

            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var services = new ServiceCollection();

            services
                .AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureFilter<TestFilter>()
                .AddFeatureVariantAssigner<TestAssigner>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();


            IEnumerable<IFeatureFilterMetadata> featureFilters = serviceProvider.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>();

            TestFilter testFeatureFilter = (TestFilter)featureFilters.First(f => f is TestFilter);

            bool called = false;

            testFeatureFilter.Callback = (evaluationContext) =>
            {
                called = true;

                return Task.FromResult(true);
            };

            await featureManager.IsEnabledAsync(WithSuffixFeature, CancellationToken.None);

            Assert.True(called);

            called = false;

            await featureManager.IsEnabledAsync(WithoutSuffixFeature, CancellationToken.None);

            Assert.True(called);
        }

        [Fact]
        public async Task Integrates()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            TestServer testServer = new TestServer(WebHost.CreateDefaultBuilder().ConfigureServices(services =>
                {
                    services
                        .AddSingleton(config)
                        .AddFeatureManagement()
                        .AddFeatureFilter<TestFilter>();

                    services.AddMvcCore(o =>
                    {
                        DisableEndpointRouting(o);
                        o.Filters.AddForFeature<MvcFilter>(ConditionalFeature);
                    });
                })
            .Configure(app =>
            {

                app.UseForFeature(ConditionalFeature, a => a.Use(async (ctx, next) =>
                {
                    ctx.Response.Headers[nameof(RouterMiddleware)] = bool.TrueString;

                    await next();
                }));

                app.UseMvc();
            }));

            IEnumerable<IFeatureFilterMetadata> featureFilters = testServer.Host.Services.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>();

            TestFilter testFeatureFilter = (TestFilter)featureFilters.First(f => f is TestFilter);

            testFeatureFilter.Callback = _ => Task.FromResult(true);

            HttpResponseMessage res = await testServer.CreateClient().GetAsync("");

            Assert.True(res.Headers.Contains(nameof(MvcFilter)));
            Assert.True(res.Headers.Contains(nameof(RouterMiddleware)));

            testFeatureFilter.Callback = _ => Task.FromResult(false);

            res = await testServer.CreateClient().GetAsync("");

            Assert.False(res.Headers.Contains(nameof(MvcFilter)));
            Assert.False(res.Headers.Contains(nameof(RouterMiddleware)));
        }

        [Fact]
        public async Task GatesFeatures()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            TestServer testServer = new TestServer(WebHost.CreateDefaultBuilder().ConfigureServices(services =>
                {
                    services
                        .AddSingleton(config)
                        .AddFeatureManagement()
                        .AddFeatureFilter<TestFilter>();

                    services.AddMvcCore(o => DisableEndpointRouting(o));
                })
            .Configure(app => app.UseMvc()));

            IEnumerable<IFeatureFilterMetadata> featureFilters = testServer.Host.Services.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>();

            TestFilter testFeatureFilter = (TestFilter)featureFilters.First(f => f is TestFilter);

            //
            // Enable all features
            testFeatureFilter.Callback = ctx => Task.FromResult(true);

            HttpResponseMessage gateAllResponse = await testServer.CreateClient().GetAsync("gateAll");
            HttpResponseMessage gateAnyResponse = await testServer.CreateClient().GetAsync("gateAny");
            HttpResponseMessage gateNotOffResponse = await testServer.CreateClient().GetAsync("gateNotOff");
            HttpResponseMessage gateNotOnResponse = await testServer.CreateClient().GetAsync("gateNotOn");

            Assert.Equal(HttpStatusCode.OK, gateAllResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, gateAnyResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, gateNotOffResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, gateNotOnResponse.StatusCode);

            //
            // Enable 1/2 features
            testFeatureFilter.Callback = ctx => Task.FromResult(ctx.FeatureFlagName == Features.ConditionalFeature);

            gateAllResponse = await testServer.CreateClient().GetAsync("gateAll");
            gateAnyResponse = await testServer.CreateClient().GetAsync("gateAny");

            Assert.Equal(HttpStatusCode.NotFound, gateAllResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, gateAnyResponse.StatusCode);

            //
            // Enable no
            testFeatureFilter.Callback = ctx => Task.FromResult(false);

            gateAllResponse = await testServer.CreateClient().GetAsync("gateAll");
            gateAnyResponse = await testServer.CreateClient().GetAsync("gateAny");

            Assert.Equal(HttpStatusCode.NotFound, gateAllResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, gateAnyResponse.StatusCode);
        }

        [Fact]
        public async Task GatesRazorPageFeatures()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            TestServer testServer = new TestServer(WebHost.CreateDefaultBuilder().ConfigureServices(services =>
            {
                services
                    .AddSingleton(config)
                    .AddFeatureManagement()
                    .AddFeatureFilter<TestFilter>();

                services.AddMvc(o => DisableEndpointRouting(o));
            })
            .Configure(app =>
            {
                app.UseMvc();
            }));

            IEnumerable<IFeatureFilterMetadata> featureFilters = testServer.Host.Services.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>();

            TestFilter testFeatureFilter = (TestFilter)featureFilters.First(f => f is TestFilter);

            //
            // Enable all features
            testFeatureFilter.Callback = ctx => Task.FromResult(true);

            HttpResponseMessage gateAllResponse = await testServer.CreateClient().GetAsync("RazorTestAll");
            HttpResponseMessage gateAnyResponse = await testServer.CreateClient().GetAsync("RazorTestAny");
            HttpResponseMessage gateNotOffResponse = await testServer.CreateClient().GetAsync("RazorTestNotOff");
            HttpResponseMessage gateNotOnResponse = await testServer.CreateClient().GetAsync("RazorTestNotOn");

            Assert.Equal(HttpStatusCode.OK, gateAllResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, gateAnyResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, gateNotOffResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, gateNotOnResponse.StatusCode);

            //
            // Enable 1/2 features
            testFeatureFilter.Callback = ctx => Task.FromResult(ctx.FeatureFlagName == Features.ConditionalFeature);

            gateAllResponse = await testServer.CreateClient().GetAsync("RazorTestAll");
            gateAnyResponse = await testServer.CreateClient().GetAsync("RazorTestAny");

            Assert.Equal(HttpStatusCode.NotFound, gateAllResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, gateAnyResponse.StatusCode);

            //
            // Enable no
            testFeatureFilter.Callback = ctx => Task.FromResult(false);

            gateAllResponse = await testServer.CreateClient().GetAsync("RazorTestAll");
            gateAnyResponse = await testServer.CreateClient().GetAsync("RazorTestAny");

            Assert.Equal(HttpStatusCode.NotFound, gateAllResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, gateAnyResponse.StatusCode);
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
                .AddFeatureManagement()
                .AddFeatureFilter<TimeWindowFilter>();

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
                .AddFeatureManagement()
                .AddFeatureFilter<PercentageFilter>();

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

            Assert.True(enabledCount > 0 && enabledCount < 10);
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
                .AddFeatureManagement()
                .AddFeatureFilter<ContextualTargetingFilter>();

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
        public async Task VariantTargeting()
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
                .AddFeatureVariantAssigner<ContextualTargetingFeatureVariantAssigner>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IDynamicFeatureManager variantManager = serviceProvider.GetRequiredService<IDynamicFeatureManager>();

            //
            // Targeted
            Assert.Equal("def", await variantManager.GetVariantAsync<string, ITargetingContext>(
                Features.ContextualVariantTargetingFeature,
                new TargetingContext
                {
                    UserId = "Jeff"
                },
                CancellationToken.None));

            //
            // Not targeted
            Assert.Equal("abc", await variantManager.GetVariantAsync<string, ITargetingContext>(
                Features.ContextualVariantTargetingFeature,
                new TargetingContext
                {
                    UserId = "Patty"
                },
                CancellationToken.None));
        }

        [Fact]
        public async Task TargetingAssignmentPrecedence()
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
                .AddFeatureVariantAssigner<ContextualTargetingFeatureVariantAssigner>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IDynamicFeatureManager variantManager = serviceProvider.GetRequiredService<IDynamicFeatureManager>();

            //
            // Assigned variant by default rollout due to no higher precedence match
            Assert.Equal("def", await variantManager.GetVariantAsync<string, ITargetingContext>(
                Features.PrecedenceTestingFeature,
                new TargetingContext
                {
                    UserId = "Patty"
                },
                CancellationToken.None));

            //
            // Assigned variant by group due to higher precedence than default rollout
            Assert.Equal("ghi", await variantManager.GetVariantAsync<string, ITargetingContext>(
                Features.PrecedenceTestingFeature,
                new TargetingContext
                {
                    UserId = "Patty",
                    Groups = new string[]
                    {
                        "Ring0"
                    }
                },
                CancellationToken.None));

            //
            // Assigned variant by user name to higher precedence than default rollout, and group match
            Assert.Equal("jkl", await variantManager.GetVariantAsync<string, ITargetingContext>(
                Features.PrecedenceTestingFeature,
                new TargetingContext
                {
                    UserId = "Jeff",
                    Groups = new string[]
                    {
                        "Ring0"
                    }
                },
                CancellationToken.None));
        }

        [Fact]
        public async Task AccumulatesAudience()
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
                .AddFeatureVariantAssigner<ContextualTargetingFeatureVariantAssigner>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IDynamicFeatureManager variantManager = serviceProvider.GetRequiredService<IDynamicFeatureManager>();

            IFeatureFlagDefinitionProvider featureProvider = serviceProvider.GetRequiredService<IFeatureFlagDefinitionProvider>();

            var occurences = new Dictionary<string, int>();

            int totalAssignments = 3000;

            //
            // Test default rollout percentage accumulation
            for (int i = 0; i < totalAssignments; i++)
            {
                string result = await variantManager.GetVariantAsync<string, ITargetingContext>(
                    "AccumulatedTargetingFeature",
                    new TargetingContext
                    {
                        UserId = RandomHelper.GetRandomString(32)
                    },
                    CancellationToken.None);

                if (!occurences.ContainsKey(result))
                {
                    occurences.Add(result, 1);
                }
                else
                {
                    occurences[result]++;
                }
            }

            foreach (KeyValuePair<string, int> occurence in occurences)
            {
                double expectedPercentage = double.Parse(occurence.Key);

                double tolerance = expectedPercentage * .25;

                double percentage = 100 * (double)occurence.Value / totalAssignments;

                Assert.True(percentage > expectedPercentage - tolerance);

                Assert.True(percentage < expectedPercentage + tolerance);
            }

            occurences.Clear();

            //
            // Test Group rollout accumulation
            for (int i = 0; i < totalAssignments; i++)
            {
                string result = await variantManager.GetVariantAsync<string, ITargetingContext>(
                    "AccumulatedGroupsTargetingFeature",
                    new TargetingContext
                    {
                        UserId = RandomHelper.GetRandomString(32),
                        Groups = new string[] { "r", }
                    },
                    CancellationToken.None);

                if (!occurences.ContainsKey(result))
                {
                    occurences.Add(result, 1);
                }
                else
                {
                    occurences[result]++;
                }
            }

            foreach (KeyValuePair<string, int> occurence in occurences)
            {
                double expectedPercentage = double.Parse(occurence.Key);

                double tolerance = expectedPercentage * .25;

                double percentage = 100 * (double)occurence.Value / totalAssignments;

                Assert.True(percentage > expectedPercentage - tolerance);

                Assert.True(percentage < expectedPercentage + tolerance);
            }
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
                .AddFeatureFilter<TargetingFilter>();

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

            Assert.False(await featureManager.IsEnabledAsync(ContextualFeature, context));

            context.AccountId = "abc";

            Assert.True(await featureManager.IsEnabledAsync(ContextualFeature, context));
        }

        [Fact]
        public async Task UsesContextVariants()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(config)
                .AddFeatureManagement()
                .AddFeatureVariantAssigner<ContextualTestAssigner>();

            ServiceProvider provider = serviceCollection.BuildServiceProvider();

            ContextualTestAssigner contextualAssigner = (ContextualTestAssigner)provider
                .GetRequiredService<IEnumerable<IFeatureVariantAssignerMetadata>>().First(f => f is ContextualTestAssigner);

            contextualAssigner.Callback = (ctx, accountContext) =>
            {
                foreach (FeatureVariant variant in ctx.FeatureDefinition.Variants)
                {
                    var allowedAccounts = new List<string>();

                    variant.AssignmentParameters.Bind("AllowedAccounts", allowedAccounts);

                    if (allowedAccounts.Contains(accountContext.AccountId))
                    {
                        return variant;
                    }
                }

                return ctx.FeatureDefinition.Variants.FirstOrDefault(v => v.Default);
            };

            IDynamicFeatureManager variantManager = provider.GetRequiredService<IDynamicFeatureManager>();

            AppContext context = new AppContext();

            context.AccountId = "NotEnabledAccount";

            Assert.Equal("abc", await variantManager.GetVariantAsync<string, IAccountContext>(
                Features.ContextualVariantFeature,
                context,
                CancellationToken.None));

            context.AccountId = "abc";

            Assert.Equal("def", await variantManager.GetVariantAsync<string, IAccountContext>(
                Features.ContextualVariantFeature,
                context,
                CancellationToken.None));
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
        public void LimitsFeatureVariantAssignerImplementations()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var serviceCollection = new ServiceCollection();

            Assert.Throws<ArgumentException>(() =>
            {
                new ServiceCollection().AddSingleton(config)
                    .AddFeatureManagement()
                    .AddFeatureVariantAssigner<InvalidFeatureVariantAssigner>();
            });

            Assert.Throws<ArgumentException>(() =>
            {
                new ServiceCollection().AddSingleton(config)
                    .AddFeatureManagement()
                    .AddFeatureVariantAssigner<InvalidFeatureVariantAssigner2>();
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

                bool hasFeatureFlags = false;

                await foreach (string feature in featureManager.GetFeatureFlagNamesAsync(CancellationToken.None))
                {
                    hasFeatureFlags = true;

                    break;
                }

                Assert.True(hasFeatureFlags);

                IDynamicFeatureManager dynamicFeatureManager = provider.GetRequiredService<IDynamicFeatureManager>();

                bool hasDynamicFeatures = false;

                await foreach (string feature in dynamicFeatureManager.GetDynamicFeatureNamesAsync(CancellationToken.None))
                {
                    hasDynamicFeatures = true;

                    break;
                }

                Assert.True(hasDynamicFeatures);
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

            FeatureManagementException e = await Assert.ThrowsAsync<FeatureManagementException>(async () =>
                await featureManager.IsEnabledAsync(ConditionalFeature, CancellationToken.None));

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

            var isEnabled = await featureManager.IsEnabledAsync(ConditionalFeature);

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
            const string DynamicFeature = "DynamicFeature";

            //
            // Feature flag
            FeatureFlagDefinition testFeature = new FeatureFlagDefinition
            {
                Name = ConditionalFeature,
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

            //
            // Dynamic feature
            DynamicFeatureDefinition dynamicFeature = new DynamicFeatureDefinition
            {
                Name = DynamicFeature,
                Assigner = "Test",
                Variants = new List<FeatureVariant>()
                {
                    new FeatureVariant
                    {
                        Name = "V1",
                        AssignmentParameters = new ConfigurationBuilder().AddInMemoryCollection(
                            new Dictionary<string, string>()
                            {
                                { "P1", "V1" }
                            })
                            .Build(),
                        ConfigurationReference = "Ref1",
                        Default = true
                    },
                    new FeatureVariant
                    {
                        Name = "V2",
                        AssignmentParameters = new ConfigurationBuilder().AddInMemoryCollection(
                            new Dictionary<string, string>()
                            {
                                { "P2", "V2" }
                            })
                            .Build(),
                        ConfigurationReference = "Ref2"
                    }
                }
            };

            var services = new ServiceCollection();

            var definitionProvider = new InMemoryFeatureDefinitionProvider(
                new FeatureFlagDefinition[]
                {
                    testFeature
                },
                new DynamicFeatureDefinition[]
                {
                    dynamicFeature
                });

            services.AddSingleton<IFeatureFlagDefinitionProvider>(definitionProvider)
                    .AddSingleton<IDynamicFeatureDefinitionProvider>(definitionProvider)
                    .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                    .AddFeatureManagement()
                    .AddFeatureFilter<TestFilter>()
                    .AddFeatureVariantAssigner<TestAssigner>();

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

                Assert.Equal(ConditionalFeature, evaluationContext.FeatureFlagName);

                return Task.FromResult(true);
            };

            await featureManager.IsEnabledAsync(ConditionalFeature);

            Assert.True(called);

            IDynamicFeatureManager dynamicFeatureManager = serviceProvider.GetRequiredService<IDynamicFeatureManager>();

            IEnumerable<IFeatureVariantAssignerMetadata> featureAssigners = serviceProvider.GetRequiredService<IEnumerable<IFeatureVariantAssignerMetadata>>();

            //
            // Sync filter
            TestAssigner testFeatureVariantAssigner = (TestAssigner)featureAssigners.First(f => f is TestAssigner);

            called = false;

            testFeatureVariantAssigner.Callback = (assignmentContext) =>
            {
                called = true;

                Assert.True(assignmentContext.FeatureDefinition.Variants.Count() == 2);

                FeatureVariant v1 = assignmentContext.FeatureDefinition.Variants.First(v => v.Name == "V1");

                Assert.True(v1.Default);

                Assert.Equal("V1", v1.AssignmentParameters["P1"]);

                Assert.Equal("Ref1", v1.ConfigurationReference);

                FeatureVariant v2 = assignmentContext.FeatureDefinition.Variants.First(v => v.Name == "V2");

                Assert.False(v2.Default);

                Assert.Equal("Ref2", v2.ConfigurationReference);

                return v1;
            };

            await dynamicFeatureManager.GetVariantAsync<string>(DynamicFeature, CancellationToken.None);

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
                tasks.Add(featureManager.IsEnabledAsync(ConditionalFeature));
            }

            Assert.True(called);

            await Task.WhenAll(tasks);

            bool result = tasks.First().Result;

            foreach (Task<bool> t in tasks)
            {
                Assert.Equal(result, t.Result);
            }
        }

        private static void DisableEndpointRouting(MvcOptions options)
        {
#if NET6_0 || NETCOREAPP3_1
            //
            // Endpoint routing is disabled by default in .NET Core 2.1 since it didn't exist.
            options.EnableEndpointRouting = false;
#endif
        }
    }
}
