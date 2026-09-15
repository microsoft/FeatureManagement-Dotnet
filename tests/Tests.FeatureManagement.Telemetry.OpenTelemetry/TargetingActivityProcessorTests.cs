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
            var exporter = new CapturingExporter<Activity>(activity => activity.TagObjects);

            using TracerProvider tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource(ActivitySourceName)
                .AddProcessor(new TargetingActivityProcessor())
                .AddProcessor(new SimpleActivityExportProcessor(exporter))
                .Build();

            using var activitySource = new ActivitySource(ActivitySourceName);

            using (Activity activity = activitySource.StartActivity("TestActivity"))
            {
                Assert.NotNull(activity);

                activity.AddBaggage(TargetingIdKey, "Bob");
            }

            tracerProvider.ForceFlush();

            IReadOnlyList<KeyValuePair<string, object>> attributes = Assert.Single(exporter.Records);

            KeyValuePair<string, object> tag = Assert.Single(attributes, t => t.Key == TargetingIdKey);

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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AutomaticallyWiredProcessorRespectsExporterRegistrationOrder(bool integrationFirst)
        {
            var exporter = new CapturingExporter<Activity>(activity => activity.TagObjects);

            const string diActivitySourceName = "TargetingActivityProcessorTests.DI";

            var services = new ServiceCollection();

            services.AddFeatureManagement();

            OpenTelemetryBuilder builder = services.AddOpenTelemetry();

            if (integrationFirst)
            {
                builder.WithFeatureManagement();
            }

            builder.WithTracing(tracing => tracing
                .AddSource(diActivitySourceName)
                .AddProcessor(new SimpleActivityExportProcessor(exporter)));

            if (!integrationFirst)
            {
                builder.WithFeatureManagement();
            }

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            TracerProvider tracerProvider = serviceProvider.GetRequiredService<TracerProvider>();

            using var activitySource = new ActivitySource(diActivitySourceName);

            using (Activity activity = activitySource.StartActivity("TestActivity"))
            {
                Assert.NotNull(activity);

                activity.AddBaggage(TargetingIdKey, "Carol");
            }

            tracerProvider.ForceFlush();

            IReadOnlyList<KeyValuePair<string, object>> attributes = Assert.Single(exporter.Records);

            if (integrationFirst)
            {
                KeyValuePair<string, object> tag = Assert.Single(attributes, t => t.Key == TargetingIdKey);

                Assert.Equal("Carol", tag.Value);
            }
            else
            {
                Assert.DoesNotContain(attributes, t => t.Key == TargetingIdKey);
            }
        }
    }
}
