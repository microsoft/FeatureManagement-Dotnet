// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Tests.FeatureManagement
{
    public class FeatureManagement
    {
        private const string OnFeature = "OnTestFeature";
        private const string OffFeature = "OffFeature";
        private const string ConditionalFeature = "ConditionalFeature";

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

                Assert.Equal(ConditionalFeature, evaluationContext.FeatureName);

                return true;
            };

            await featureManager.IsEnabledAsync(ConditionalFeature);

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

                services.AddMvcCore(o => {

                    o.Filters.AddForFeature<MvcFilter>(ConditionalFeature);
                });
            })
            .Configure(app => {

                app.UseForFeature(ConditionalFeature, a => a.Use(async (ctx, next) =>
                {
                    ctx.Response.Headers[nameof(RouterMiddleware)] = bool.TrueString;

                    await next();
                }));

                app.UseMvc();
            }));

            IEnumerable<IFeatureFilterMetadata> featureFilters = testServer.Host.Services.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>();

            TestFilter testFeatureFilter = (TestFilter)featureFilters.First(f => f is TestFilter);

            testFeatureFilter.Callback = _ => true;

            HttpResponseMessage res = await testServer.CreateClient().GetAsync("");

            Assert.True(res.Headers.Contains(nameof(MvcFilter)));
            Assert.True(res.Headers.Contains(nameof(RouterMiddleware)));

            testFeatureFilter.Callback = _ => false;

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

                services.AddMvcCore();
            })
            .Configure(app => app.UseMvc()));

            IEnumerable<IFeatureFilterMetadata> featureFilters = testServer.Host.Services.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>();

            TestFilter testFeatureFilter = (TestFilter)featureFilters.First(f => f is TestFilter);

            //
            // Enable all features
            testFeatureFilter.Callback = ctx => true;

            HttpResponseMessage gateAllResponse = await testServer.CreateClient().GetAsync("gateAll");
            HttpResponseMessage gateAnyResponse = await testServer.CreateClient().GetAsync("gateAny");

            Assert.Equal(HttpStatusCode.OK, gateAllResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, gateAnyResponse.StatusCode);

            //
            // Enable 1/2 features
            testFeatureFilter.Callback = ctx => ctx.FeatureName == Enum.GetName(typeof(Features), Features.ConditionalFeature);

            gateAllResponse = await testServer.CreateClient().GetAsync("gateAll");
            gateAnyResponse = await testServer.CreateClient().GetAsync("gateAny");

            Assert.Equal(HttpStatusCode.NotFound, gateAllResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, gateAnyResponse.StatusCode);

            //
            // Enable no
            testFeatureFilter.Callback = ctx => false;

            gateAllResponse = await testServer.CreateClient().GetAsync("gateAll");
            gateAnyResponse = await testServer.CreateClient().GetAsync("gateAny");

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

            Assert.False(await featureManager.IsEnabledAsync(ConditionalFeature, context));

            context.AccountId = "abc";

            Assert.True(await featureManager.IsEnabledAsync(ConditionalFeature, context));
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
    }
}
