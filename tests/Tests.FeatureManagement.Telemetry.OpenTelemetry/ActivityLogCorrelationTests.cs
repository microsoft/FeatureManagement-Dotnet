// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using OpenTelemetry.Logs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Tests.FeatureManagement.Telemetry.OpenTelemetry
{
    public class ActivityLogCorrelationTests
    {
        private const string AzureMonitorCustomEventNameKey = "microsoft.custom_event.name";
        private const string FeatureEvaluationEventName = "FeatureEvaluation";
        private const string FeatureManagementActivitySourceName = "Microsoft.FeatureManagement";

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task ExportedLogRecordCarriesCustomEventNameTypedAttributesAndRecordedContext(bool hasParent, bool parentRecorded)
        {
            var exportedLogRecords = new List<LogRecord>();

            var services = new ServiceCollection();

            services.AddFeatureManagement();

            services.AddOpenTelemetry().WithFeatureManagement();

            services.AddLogging(builder =>
                builder.AddOpenTelemetry(logging => logging.AddInMemoryExporter(exportedLogRecords)));

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            foreach (IHostedService hostedService in serviceProvider.GetServices<IHostedService>())
            {
                await hostedService.StartAsync(default);
            }

            Assert.Null(Activity.Current);

            using Activity parent = hasParent ? new Activity("Parent").SetIdFormat(ActivityIdFormat.W3C).Start() : null;

            if (parent != null)
            {
                parent.ActivityTraceFlags = parentRecorded ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None;
            }

            ActivityTraceId evaluationTraceId;
            ActivitySpanId evaluationSpanId;

            using (var activitySource = new ActivitySource(FeatureManagementActivitySourceName))
            using (Activity activity = activitySource.StartActivity("FeatureEvaluation"))
            {
                Assert.NotNull(activity);
                Assert.True(activity.Recorded);
                Assert.True(activity.IsAllDataRequested);

                evaluationTraceId = activity.TraceId;
                evaluationSpanId = activity.SpanId;

                if (parent != null)
                {
                    Assert.Equal(parent.TraceId, activity.TraceId);
                    Assert.Equal(parent.SpanId, activity.ParentSpanId);
                }

                var tags = new ActivityTagsCollection
                {
                    { "FeatureName", "TestFeature" },
                    { "Enabled", true },
                    { "TargetingId", "test-user" },
                    { "VariantAssignmentPercentage", 25.5 }
                };

                activity.AddEvent(new ActivityEvent("FeatureFlag", DateTimeOffset.UtcNow, tags));
            }

            // Force the batch/simple export pipeline to flush before asserting.
            serviceProvider.GetRequiredService<LoggerProvider>().ForceFlush();

            LogRecord logRecord = Assert.Single(exportedLogRecords);

            Assert.NotEqual(default(ActivitySpanId), logRecord.SpanId);
            Assert.Equal(evaluationTraceId, logRecord.TraceId);
            Assert.Equal(evaluationSpanId, logRecord.SpanId);
            Assert.Equal(ActivityTraceFlags.Recorded, logRecord.TraceFlags);
            Assert.Same(parent, Activity.Current);

            if (parent != null)
            {
                Assert.Equal(parentRecorded, parent.Recorded);
            }

            Assert.Equal(FeatureEvaluationEventName, logRecord.EventId.Name);

            IReadOnlyList<KeyValuePair<string, object>> attributes = logRecord.Attributes;

            Assert.NotNull(attributes);

            Assert.Contains(attributes, kvp => kvp.Key == AzureMonitorCustomEventNameKey && (string)kvp.Value == FeatureEvaluationEventName);

            KeyValuePair<string, object> enabledAttribute = Assert.Single(attributes, kvp => kvp.Key == "Enabled");

            Assert.IsType<bool>(enabledAttribute.Value);

            Assert.True((bool)enabledAttribute.Value);

            KeyValuePair<string, object> targetingIdAttribute = Assert.Single(attributes, kvp => kvp.Key == "TargetingId");

            Assert.IsType<string>(targetingIdAttribute.Value);

            Assert.Equal("test-user", targetingIdAttribute.Value);

            KeyValuePair<string, object> percentageAttribute = Assert.Single(attributes, kvp => kvp.Key == "VariantAssignmentPercentage");

            Assert.IsType<double>(percentageAttribute.Value);

            Assert.Equal(25.5, (double)percentageAttribute.Value);
        }
    }
}
