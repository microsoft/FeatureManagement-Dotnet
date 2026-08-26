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

        [Fact]
        public async Task ExportedLogRecordCarriesCustomEventNameAndTypedAttributes()
        {
            var exportedLogRecords = new List<LogRecord>();

            var services = new ServiceCollection();

            services.AddLogging(builder =>
                builder.AddOpenTelemetry(logging => logging.AddInMemoryExporter(exportedLogRecords)));

            services.AddFeatureManagement().AddOpenTelemetry();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            foreach (IHostedService hostedService in serviceProvider.GetServices<IHostedService>())
            {
                await hostedService.StartAsync(default);
            }

            using (var activitySource = new ActivitySource(FeatureManagementActivitySourceName))
            using (Activity activity = activitySource.StartActivity("FeatureEvaluation"))
            {
                Assert.NotNull(activity);

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
