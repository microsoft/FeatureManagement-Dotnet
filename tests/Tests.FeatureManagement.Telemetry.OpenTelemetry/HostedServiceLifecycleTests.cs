// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Tests.FeatureManagement.Telemetry.OpenTelemetry
{
    public class HostedServiceLifecycleTests
    {
        private const string FeatureManagementActivitySourceName = "Microsoft.FeatureManagement";

        [Fact]
        public async Task PublisherIsNotConstructedUntilHostedServiceStarts()
        {
            var loggerProvider = new CapturingLoggerProvider();

            var services = new ServiceCollection();

            services.AddLogging(builder => builder.AddProvider(loggerProvider));

            services.AddFeatureManagement().AddOpenTelemetry();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            // No ActivityListener has been registered yet, since the hosted service (and thus the
            // publisher's constructor) has not run. The ActivitySource should have no listeners,
            // so starting an activity returns null and no event can be emitted.
            using (var activitySource = new ActivitySource(FeatureManagementActivitySourceName))
            using (Activity activity = activitySource.StartActivity("FeatureEvaluation"))
            {
                Assert.Null(activity);
            }

            Assert.Empty(loggerProvider.Records);

            // Starting the hosted service constructs the publisher via GetRequiredService, which
            // registers the ActivityListener.
            foreach (IHostedService hostedService in serviceProvider.GetServices<IHostedService>())
            {
                await hostedService.StartAsync(default);
            }

            EmitFeatureFlagActivityEvent();

            Assert.Single(loggerProvider.Records);
        }

        [Fact]
        public async Task ListenerIsDisposedWhenServiceProviderIsDisposed()
        {
            var loggerProvider = new CapturingLoggerProvider();

            var services = new ServiceCollection();

            services.AddLogging(builder => builder.AddProvider(loggerProvider));

            services.AddFeatureManagement().AddOpenTelemetry();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            foreach (IHostedService hostedService in serviceProvider.GetServices<IHostedService>())
            {
                await hostedService.StartAsync(default);
            }

            EmitFeatureFlagActivityEvent();

            Assert.Single(loggerProvider.Records);

            foreach (IHostedService hostedService in serviceProvider.GetServices<IHostedService>())
            {
                await hostedService.StopAsync(default);
            }

            // Disposing the container disposes the OpenTelemetryEventPublisher singleton, which
            // disposes its ActivityListener.
            serviceProvider.Dispose();

            using (var activitySource = new ActivitySource(FeatureManagementActivitySourceName))
            using (Activity activity = activitySource.StartActivity("FeatureEvaluation"))
            {
                Assert.Null(activity);
            }

            // No new record should have been captured after disposal.
            Assert.Single(loggerProvider.Records);
        }

        private static void EmitFeatureFlagActivityEvent()
        {
            using var activitySource = new ActivitySource(FeatureManagementActivitySourceName);

            using Activity activity = activitySource.StartActivity("FeatureEvaluation");

            Assert.NotNull(activity);

            var tags = new ActivityTagsCollection
            {
                { "FeatureName", "TestFeature" },
                { "Enabled", true }
            };

            activity.AddEvent(new ActivityEvent("FeatureFlag", DateTimeOffset.UtcNow, tags));
        }
    }
}
