// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Telemetry.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace Tests.FeatureManagement.Telemetry.OpenTelemetry
{
    public class TargetingActivityProcessorTests
    {
        private const string TargetingIdKey = "TargetingId";
        private const string ActivitySourceName = "TargetingActivityProcessorTests";

        [Fact]
        public void NullActivityIsNoOp()
        {
            var processor = new TargetingActivityProcessor();

            Exception exception = Record.Exception(() => processor.OnEnd(null));

            Assert.Null(exception);
        }

        [Fact]
        public void BaggagePresentAddsTargetingIdTag()
        {
            var processor = new TargetingActivityProcessor();

            using var activitySource = new ActivitySource(ActivitySourceName);

            using ActivityListener listener = CreateAllDataListener(activitySource);

            using Activity activity = activitySource.StartActivity("TestActivity");

            Assert.NotNull(activity);

            activity.AddBaggage(TargetingIdKey, "Alice");

            processor.OnEnd(activity);

            KeyValuePair<string, object> tag = Assert.Single(activity.TagObjects, t => t.Key == TargetingIdKey);

            Assert.Equal("Alice", tag.Value);
        }

        [Fact]
        public void BaggageAbsentDoesNotAddTargetingIdTag()
        {
            var processor = new TargetingActivityProcessor();

            using var activitySource = new ActivitySource(ActivitySourceName);

            using ActivityListener listener = CreateAllDataListener(activitySource);

            using Activity activity = activitySource.StartActivity("TestActivity");

            Assert.NotNull(activity);

            processor.OnEnd(activity);

            Assert.DoesNotContain(activity.TagObjects, t => t.Key == TargetingIdKey);
        }

        [Fact]
        public void EmptyBaggageValueDoesNotAddTargetingIdTag()
        {
            var processor = new TargetingActivityProcessor();

            using var activitySource = new ActivitySource(ActivitySourceName);

            using ActivityListener listener = CreateAllDataListener(activitySource);

            using Activity activity = activitySource.StartActivity("TestActivity");

            Assert.NotNull(activity);

            activity.AddBaggage(TargetingIdKey, string.Empty);

            processor.OnEnd(activity);

            Assert.DoesNotContain(activity.TagObjects, t => t.Key == TargetingIdKey);
        }

        [Fact]
        public void ExportedActivityAlreadyContainsTargetingIdWhenProcessorIsRegisteredBeforeExporter()
        {
            var exportedActivities = new List<Activity>();

            using TracerProvider tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource(ActivitySourceName)
                // Register TargetingActivityProcessor before the in-memory exporter so it observes
                // (and enriches) the activity before the exporter does, matching the fix applied to
                // the sample apps' TracerProviderBuilder wiring.
                .AddProcessor(new TargetingActivityProcessor())
                .AddInMemoryExporter(exportedActivities)
                .Build();

            using var activitySource = new ActivitySource(ActivitySourceName);

            using (Activity activity = activitySource.StartActivity("TestActivity"))
            {
                Assert.NotNull(activity);

                activity.AddBaggage(TargetingIdKey, "Bob");
            }

            tracerProvider.ForceFlush();

            Activity exportedActivity = Assert.Single(exportedActivities);

            KeyValuePair<string, object> tag = Assert.Single(exportedActivity.TagObjects, t => t.Key == TargetingIdKey);

            Assert.Equal("Bob", tag.Value);
        }

        private static ActivityListener CreateAllDataListener(ActivitySource activitySource)
        {
            var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == activitySource.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
            };

            ActivitySource.AddActivityListener(listener);

            return listener;
        }

        [Fact]
        public void ExportedActivityIsEnrichedWithTargetingIdWhenProcessorIsAutomaticallyWired()
        {
            var exportedActivities = new List<Activity>();

            const string diActivitySourceName = "TargetingActivityProcessorTests.DI.BeforeExporters";

            var services = new ServiceCollection();

            services.AddFeatureManagement().AddOpenTelemetry();

            services.AddOpenTelemetry()
                .WithTracing(tracing => tracing
                    .AddSource(diActivitySourceName)
                    .AddInMemoryExporter(exportedActivities));

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            TracerProvider tracerProvider = serviceProvider.GetRequiredService<TracerProvider>();

            using var activitySource = new ActivitySource(diActivitySourceName);

            using (Activity activity = activitySource.StartActivity("TestActivity"))
            {
                Assert.NotNull(activity);

                activity.AddBaggage(TargetingIdKey, "Carol");
            }

            tracerProvider.ForceFlush();

            Activity exportedActivity = Assert.Single(exportedActivities);

            KeyValuePair<string, object> tag = Assert.Single(exportedActivity.TagObjects, t => t.Key == TargetingIdKey);

            Assert.Equal("Carol", tag.Value);
        }

        [Fact]
        public void ExportedActivityIsEnrichedWithTargetingIdWhenAddOpenTelemetryIsCalledAfterTracingIsConfigured()
        {
            var exportedActivities = new List<Activity>();

            const string diActivitySourceName = "TargetingActivityProcessorTests.DI.AfterExporters";

            var services = new ServiceCollection();

            // Configure tracing/exporters BEFORE AddFeatureManagement().AddOpenTelemetry() (the
            // opposite order from the test above) to prove that TargetingActivityProcessor still
            // runs ahead of the exporter regardless of call order.
            services.AddOpenTelemetry()
                .WithTracing(tracing => tracing
                    .AddSource(diActivitySourceName)
                    .AddInMemoryExporter(exportedActivities));

            services.AddFeatureManagement().AddOpenTelemetry();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            TracerProvider tracerProvider = serviceProvider.GetRequiredService<TracerProvider>();

            using var activitySource = new ActivitySource(diActivitySourceName);

            using (Activity activity = activitySource.StartActivity("TestActivity"))
            {
                Assert.NotNull(activity);

                activity.AddBaggage(TargetingIdKey, "Dana");
            }

            tracerProvider.ForceFlush();

            Activity exportedActivity = Assert.Single(exportedActivities);

            KeyValuePair<string, object> tag = Assert.Single(exportedActivity.TagObjects, t => t.Key == TargetingIdKey);

            Assert.Equal("Dana", tag.Value);
        }
    }
}
