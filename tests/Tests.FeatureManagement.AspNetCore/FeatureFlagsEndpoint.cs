#if NET7_0_OR_GREATER
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
        public ValueTask<TargetingContext> GetContextAsync()
        {
            return new ValueTask<TargetingContext>(new TargetingContext
            {
                UserId = "testUser",
                Groups = new[] { "testGroup" }
            });
        }
    }

    public class FeatureTestServer : IDisposable
    {
        private readonly IHost _host;
        private readonly HttpClient _client;
        private readonly bool _featureEnabled;

        public FeatureTestServer(bool featureEnabled = true)
        {
            _featureEnabled = featureEnabled;
            _host = CreateHostBuilder().Build();
            _host.Start();
            _client = _host.GetTestServer().CreateClient();
        }

        private IHostBuilder CreateHostBuilder()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["FeatureManagement:TestFeature"] = _featureEnabled.ToString().ToLower()
                })
                .Build();

            return Host.CreateDefaultBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.AddSingleton<IConfiguration>(configuration);
                            services.AddSingleton<ITargetingContextAccessor, TestTargetingContextAccessor>();
                            services.AddFeatureManagement();
                            services.AddRouting();
                        })
                        .Configure(app =>
                        {
                            app.UseRouting();
                            app.UseEndpoints(endpoints =>
                            {
                                endpoints.MapGet("/test", new Func<string>(() => "Feature Enabled"))
                                    .WithFeatureFlag("TestFeature", () =>
                                        new TargetingContext
                                        {
                                            UserId = "testUser",
                                            Groups = new[] { "testGroup" }
                                        });
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
            using var server = new FeatureTestServer(featureEnabled: true);
            var response = await server.Client.GetAsync("/test");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task WhenFeatureDisabled_ReturnsNotFound()
        {
            using var server = new FeatureTestServer(featureEnabled: false);
            var response = await server.Client.GetAsync("/test");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
#endif
