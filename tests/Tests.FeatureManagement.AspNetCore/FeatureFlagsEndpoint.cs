// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.AspNetCore;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Tests.FeatureManagement.AspNetCore
{
    public class TestTargetingContextAccessor : ITargetingContextAccessor
    {
        private readonly string _userId;
        private readonly string[] _groups;

        public TestTargetingContextAccessor(string userId = "testUser", string[] groups = null)
        {
            _userId = userId;
            _groups = groups ?? new[] { "testGroup" };
        }

        public ValueTask<TargetingContext> GetContextAsync()
        {
            var context = new TargetingContext
            {
                UserId = _userId,
                Groups = _groups
            };
            return new ValueTask<TargetingContext>(context);
        }
    }

    public class FeatureTestServer : IDisposable
    {
        private readonly IHost _host;
        private readonly HttpClient _client;
        private readonly IDictionary<string, string> _featureSettings;
        private readonly ITargetingContextAccessor _targetingContextAccessor;

        public FeatureTestServer(
            IDictionary<string, string> featureSettings = null,
            ITargetingContextAccessor targetingContextAccessor = null)
        {
            _featureSettings = featureSettings ?? new Dictionary<string, string>
            {
                ["FeatureManagement:TestFeature"] = "true"
            };
            _targetingContextAccessor = targetingContextAccessor ?? new TestTargetingContextAccessor();
            _host = CreateHostBuilder().Build();
            _host.Start();
            _client = _host.GetTestServer().CreateClient();
        }

        private IHostBuilder CreateHostBuilder()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(_featureSettings)
                .Build();

            return Host.CreateDefaultBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.AddSingleton<IConfiguration>(configuration);
                            services.AddSingleton(_targetingContextAccessor);
                            services.AddFeatureManagement()
                                    .AddFeatureFilter<TargetingFilter>();
                            services.AddRouting();
                        })
                        .Configure(app =>
                        {
                            app.UseRouting();
                            app.UseMiddleware<TargetingHttpContextMiddleware>();
                            app.UseEndpoints(endpoints =>
                            {
                                endpoints.MapGet("/test", new Func<string>(() => "Feature Enabled"))
                                    .WithFeatureGate("TestFeature");
                                endpoints.MapGet("/test-targeting", new Func<string>(() => "Feature With Targeting Enabled"))
                                    .WithFeatureGate("TestFeatureWithTargeting");
                            });
                        });
                });
        }

        public HttpClient Client => _client;

        public void Dispose()
        {
            _host?.Dispose();
            _client?.Dispose();
        }
    }

    public class FeatureFlagsEndpointFilterTests
    {
        [Fact]
        public async Task WhenFeatureEnabled_ReturnsSuccess()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:TestFeature"] = "true"
            };

            using var server = new FeatureTestServer(featureSettings: settings);
            var response = await server.Client.GetAsync("/test");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task WhenFeatureDisabled_ReturnsNotFound()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:TestFeature"] = "false"
            };

            using var server = new FeatureTestServer(featureSettings: settings);
            var response = await server.Client.GetAsync("/test");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task WhenTargetingEnabled_AndUserInTarget_ReturnsSuccess()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:TestFeatureWithTargeting:EnabledFor:0:Name"] = "Targeting",
                ["FeatureManagement:TestFeatureWithTargeting:EnabledFor:0:Parameters:Audience:Users:0"] = "targetUser",
                ["FeatureManagement:TestFeature:EnabledFor:0:Parameters:Audience:Groups:0:Name"] = "targetGroup",
                ["FeatureManagement:TestFeature:EnabledFor:0:Parameters:Audience:Groups:0:RolloutPercentage"] = "100",
            };

            var targetingAccessor = new TestTargetingContextAccessor(
                userId: "targetUser",
                groups: new[] { "targetGroup" }
            );
            using var server = new FeatureTestServer(
                featureSettings: settings,
                targetingContextAccessor: targetingAccessor
            );
            var response = await server.Client.GetAsync("/test-targeting");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task WhenTargetingEnabled_AndUserNotInTarget_ReturnsNotFound()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:TestFeatureWithTargeting:EnabledFor:0:Name"] = "Targeting",
                ["FeatureManagement:TestFeatureWithTargeting:EnabledFor:0:Parameters:Audience:Users:0"] = "targetUser",
                ["FeatureManagement:TestFeature:EnabledFor:0:Parameters:Audience:Groups:0:Name"] = "targetGroup",
                ["FeatureManagement:TestFeature:EnabledFor:0:Parameters:Audience:Groups:0:RolloutPercentage"] = "100",
            };

            var targetingAccessor = new TestTargetingContextAccessor(
                userId: "nonTargetUser",
                groups: new[] { "nonTargetGroup" }
            );
            using var server = new FeatureTestServer(
                featureSettings: settings,
                targetingContextAccessor: targetingAccessor
            );
            var response = await server.Client.GetAsync("/test-targeting");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
