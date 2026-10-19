// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
        private readonly Action<IEndpointRouteBuilder> _endpointConfiguration;

        public FeatureTestServer(
            IDictionary<string, string> featureSettings = null,
            ITargetingContextAccessor targetingContextAccessor = null,
            Action<IEndpointRouteBuilder> endpointConfiguration = null)
        {
            _featureSettings = featureSettings ?? new Dictionary<string, string>
            {
                ["FeatureManagement:TestFeature"] = "true"
            };
            _targetingContextAccessor = targetingContextAccessor ?? new TestTargetingContextAccessor();
            _endpointConfiguration = endpointConfiguration ?? DefaultEndpointConfiguration;
            _host = CreateHostBuilder().Build();
            _host.Start();
            _client = _host.GetTestServer().CreateClient();
        }

        private void DefaultEndpointConfiguration(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/test", new Func<string>(() => "Feature Enabled"))
                .WithFeatureGate("TestFeature");
            endpoints.MapGet("/test-targeting", new Func<string>(() => "Feature With Targeting Enabled"))
                .WithFeatureGate("TestFeatureWithTargeting");
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
                            app.UseEndpoints(_endpointConfiguration);
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

    public class FeatureGateEndpointFilterTests
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
        public async Task WhenMultipleFeatures_AllEnabled_ReturnsSuccess()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:TestFeature1"] = "true",
                ["FeatureManagement:TestFeature2"] = "true"
            };

            using var server = new FeatureTestServer(
                featureSettings: settings,
                endpointConfiguration: endpoints =>
                {
                    endpoints.MapGet("/test-multiple", new Func<string>(() => "Multiple Features Enabled"))
                        .WithFeatureGate("TestFeature1", "TestFeature2");
                });

            var response = await server.Client.GetAsync("/test-multiple");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task WhenMultipleFeatures_OneDisabled_ReturnsNotFound()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:TestFeature1"] = "true",
                ["FeatureManagement:TestFeature2"] = "false"
            };

            using var server = new FeatureTestServer(
                featureSettings: settings,
                endpointConfiguration: endpoints =>
                {
                    endpoints.MapGet("/test-multiple", new Func<string>(() => "Multiple Features Enabled"))
                        .WithFeatureGate("TestFeature1", "TestFeature2");
                });

            var response = await server.Client.GetAsync("/test-multiple");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task WhenRequirementTypeAny_OneEnabled_ReturnsSuccess()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:TestFeature1"] = "true",
                ["FeatureManagement:TestFeature2"] = "false"
            };

            using var server = new FeatureTestServer(
                featureSettings: settings,
                endpointConfiguration: endpoints =>
                {
                    endpoints.MapGet("/test-any", new Func<string>(() => "Any Feature Enabled"))
                        .WithFeatureGate(RequirementType.Any, "TestFeature1", "TestFeature2");
                });

            var response = await server.Client.GetAsync("/test-any");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task WhenRequirementTypeAny_AllDisabled_ReturnsNotFound()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:TestFeature1"] = "false",
                ["FeatureManagement:TestFeature2"] = "false"
            };

            using var server = new FeatureTestServer(
                featureSettings: settings,
                endpointConfiguration: endpoints =>
                {
                    endpoints.MapGet("/test-any", new Func<string>(() => "Any Feature Enabled"))
                        .WithFeatureGate(RequirementType.Any, "TestFeature1", "TestFeature2");
                });

            var response = await server.Client.GetAsync("/test-any");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task WhenNegated_FeatureDisabled_ReturnsSuccess()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:TestFeature"] = "false"
            };

            using var server = new FeatureTestServer(
                featureSettings: settings,
                endpointConfiguration: endpoints =>
                {
                    endpoints.MapGet("/test-negated", new Func<string>(() => "Negated Feature"))
                        .WithFeatureGate(true, "TestFeature");
                });

            var response = await server.Client.GetAsync("/test-negated");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task WhenNegated_FeatureEnabled_ReturnsNotFound()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:TestFeature"] = "false"
            };

            using var server = new FeatureTestServer(
                featureSettings: settings,
                endpointConfiguration: endpoints =>
                {
                    endpoints.MapGet("/test-negated", new Func<string>(() => "Negated Feature"))
                        .WithFeatureGate(RequirementType.All, true, "TestFeature");
                });

            var response = await server.Client.GetAsync("/test-negated");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task WhenNegatedWithMultipleFeatures_AllDisabled_ReturnsSuccess()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:TestFeature1"] = "false",
                ["FeatureManagement:TestFeature2"] = "false"
            };

            using var server = new FeatureTestServer(
                featureSettings: settings,
                endpointConfiguration: endpoints =>
                {
                    endpoints.MapGet("/test-negated-multiple", new Func<string>(() => "Negated Multiple Features"))
                        .WithFeatureGate(RequirementType.All, true, "TestFeature1", "TestFeature2");
                });

            var response = await server.Client.GetAsync("/test-negated-multiple");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
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

        [Fact]
        public async Task WhenGroupFeatureEnabled_AllEndpointsInGroup_ReturnSuccess()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:GroupFeature"] = "true"
            };

            using var server = new FeatureTestServer(
                featureSettings: settings,
                endpointConfiguration: endpoints =>
                {
                    var group = endpoints.MapGroup("/api");
                    group.WithFeatureGate("GroupFeature");
                    group.MapGet("/endpoint1", new Func<string>(() => "Endpoint 1"));
                    group.MapGet("/endpoint2", new Func<string>(() => "Endpoint 2"));
                });

            var response1 = await server.Client.GetAsync("/api/endpoint1");
            var response2 = await server.Client.GetAsync("/api/endpoint2");

            Assert.Equal(System.Net.HttpStatusCode.OK, response1.StatusCode);
            Assert.Equal(System.Net.HttpStatusCode.OK, response2.StatusCode);
        }

        [Fact]
        public async Task WhenGroupFeatureDisabled_AllEndpointsInGroup_ReturnNotFound()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:GroupFeature"] = "false"
            };

            using var server = new FeatureTestServer(
                featureSettings: settings,
                endpointConfiguration: endpoints =>
                {
                    var group = endpoints.MapGroup("/api");
                    group.WithFeatureGate("GroupFeature");

                    group.MapGet("/endpoint1", new Func<string>(() => "Endpoint 1"));
                    group.MapGet("/endpoint2", new Func<string>(() => "Endpoint 2"));
                });

            var response1 = await server.Client.GetAsync("/api/endpoint1");
            var response2 = await server.Client.GetAsync("/api/endpoint2");

            Assert.Equal(System.Net.HttpStatusCode.NotFound, response1.StatusCode);
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response2.StatusCode);
        }

        [Fact]
        public async Task WhenNestedGroups_WithMultipleFeatures_ReturnsExpectedResults()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:ParentFeature"] = "true",
                ["FeatureManagement:ChildFeature"] = "false"
            };

            using var server = new FeatureTestServer(
                featureSettings: settings,
                endpointConfiguration: endpoints =>
                {
                    var parentGroup = endpoints.MapGroup("/parent");
                    parentGroup.WithFeatureGate("ParentFeature");

                    var childGroup = parentGroup.MapGroup("/child");
                    childGroup.WithFeatureGate("ChildFeature");

                    parentGroup.MapGet("/endpoint", new Func<string>(() => "Parent Endpoint"));
                    childGroup.MapGet("/endpoint", new Func<string>(() => "Child Endpoint"));
                });

            var parentResponse = await server.Client.GetAsync("/parent/endpoint");
            var childResponse = await server.Client.GetAsync("/parent/child/endpoint");

            Assert.Equal(System.Net.HttpStatusCode.OK, parentResponse.StatusCode);
            Assert.Equal(System.Net.HttpStatusCode.NotFound, childResponse.StatusCode);
        }

        [Fact]
        public async Task WhenGroupWithRequirementTypeAny_OneFeatureEnabled_ReturnsSuccess()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:Feature1"] = "true",
                ["FeatureManagement:Feature2"] = "false"
            };

            using var server = new FeatureTestServer(
                featureSettings: settings,
                endpointConfiguration: endpoints =>
                {
                    var group = endpoints.MapGroup("/api");
                    group.WithFeatureGate(RequirementType.Any, "Feature1", "Feature2");

                    group.MapGet("/endpoint", new Func<string>(() => "Any Feature Endpoint"));
                });

            var response = await server.Client.GetAsync("/api/endpoint");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task WhenGroupWithTargeting_AndUserInTarget_ReturnsSuccess()
        {
            var settings = new Dictionary<string, string>
            {
                ["FeatureManagement:GroupTargetFeature:EnabledFor:0:Name"] = "Targeting",
                ["FeatureManagement:GroupTargetFeature:EnabledFor:0:Parameters:Audience:Users:0"] = "targetUser"
            };

            var targetingAccessor = new TestTargetingContextAccessor(userId: "targetUser");

            using var server = new FeatureTestServer(
                featureSettings: settings,
                targetingContextAccessor: targetingAccessor,
                endpointConfiguration: endpoints =>
                {
                    var group = endpoints.MapGroup("/api");
                    group.WithFeatureGate("GroupTargetFeature");

                    group.MapGet("/targeted", new Func<string>(() => "Targeted Endpoint"));
                });

            var response = await server.Client.GetAsync("/api/targeted");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
    }
}
