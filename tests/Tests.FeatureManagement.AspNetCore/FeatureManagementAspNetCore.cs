// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc.TagHelpers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Tests.FeatureManagement.AspNetCore
{
    public class FeatureManagementAspNetCore
    {
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
                    o.Filters.AddForFeature<MvcFilter>(Features.ConditionalFeature);
                });
            })
            .Configure(app =>
            {
                app.UseForFeature(Features.ConditionalFeature, a => a.Use(async (ctx, next) =>
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

            Assert.Equal(HttpStatusCode.OK, gateAllResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, gateAnyResponse.StatusCode);

            //
            // Enable 1/2 features
            testFeatureFilter.Callback = ctx => Task.FromResult(ctx.FeatureName == Features.ConditionalFeature);

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

                services.AddRazorPages();

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

            Assert.Equal(HttpStatusCode.OK, gateAllResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, gateAnyResponse.StatusCode);

            //
            // Enable 1/2 features
            testFeatureFilter.Callback = ctx => Task.FromResult(ctx.FeatureName == Features.ConditionalFeature);

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

        private static void DisableEndpointRouting(MvcOptions options)
        {
            options.EnableEndpointRouting = false;
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
        public async Task CustomIFeatureManagerAspNetCoreTest()
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

            // FeatureTagHelper should use available IFeatureManager
            FeatureTagHelper featureTagHelper = new FeatureTagHelper(serviceProvider.GetRequiredService<IFeatureManagerSnapshot>(), serviceProvider.GetRequiredService<IVariantFeatureManagerSnapshot>());
            TagHelperOutput tagHelperOutput = new TagHelperOutput("TestTag", new TagHelperAttributeList(), (aBool, aHtmlEncoder) => { return null; });

            // Test returns true, so it shouldn't be modified
            featureTagHelper.Name = "Test";
            Assert.False(tagHelperOutput.IsContentModified);
            await featureTagHelper.ProcessAsync(null, tagHelperOutput);
            Assert.False(tagHelperOutput.IsContentModified);

            tagHelperOutput.Reinitialize("TestTag", TagMode.StartTagAndEndTag);

            // NotTest returns false, so it should be modified
            featureTagHelper.Name = "NotTest";
            Assert.False(tagHelperOutput.IsContentModified);
            await featureTagHelper.ProcessAsync(null, tagHelperOutput);
            Assert.True(tagHelperOutput.IsContentModified);

            tagHelperOutput.Reinitialize("TestTag", TagMode.StartTagAndEndTag);

            // When variant is used, Test flag should no longer exist and return false
            featureTagHelper.Name = "Test";
            featureTagHelper.Variant = "Something";
            Assert.False(tagHelperOutput.IsContentModified);
            await featureTagHelper.ProcessAsync(null, tagHelperOutput);
            Assert.True(tagHelperOutput.IsContentModified);
        }
    }
}
