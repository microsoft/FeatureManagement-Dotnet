// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Telemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests.FeatureManagement.Telemetry.OpenTelemetry
{
    /// <summary>
    /// Tests that assert on the individual attribute values/types produced by <see cref="OpenTelemetryEventPublisher"/>,
    /// </summary>
    public class EventPropertiesTests
    {
        private const string AzureMonitorCustomEventNameKey = "microsoft.custom_event.name";
        private const string FeatureEvaluationEventName = "FeatureEvaluation";
        private const string FeatureManagementActivitySourceName = "Microsoft.FeatureManagement";

        [Theory]
        [InlineData(VariantAssignmentReason.None, "None")]
        [InlineData(VariantAssignmentReason.DefaultWhenDisabled, "DefaultWhenDisabled")]
        [InlineData(VariantAssignmentReason.DefaultWhenEnabled, "DefaultWhenEnabled")]
        [InlineData(VariantAssignmentReason.User, "User")]
        [InlineData(VariantAssignmentReason.Group, "Group")]
        [InlineData(VariantAssignmentReason.Percentile, "Percentile")]
        public async Task VariantAssignmentReasonIsConvertedToExpectedStringValue(VariantAssignmentReason reason, string expected)
        {
            (CapturingLoggerProvider loggerProvider, ServiceProvider serviceProvider) = await CreateAndStartHostAsync();

            using (serviceProvider)
            {
                EmitFeatureFlagActivityEvent(new ActivityTagsCollection
                {
                    { "FeatureName", "TestFeature" },
                    { "Enabled", true },
                    { "VariantAssignmentReason", reason }
                });

                CapturedLogRecord record = Assert.Single(loggerProvider.Records, r => r.EventId.Name == FeatureEvaluationEventName);

                KeyValuePair<string, object> reasonAttribute = Assert.Single(record.State, kvp => kvp.Key == "VariantAssignmentReason");

                Assert.IsType<string>(reasonAttribute.Value);

                Assert.Equal(expected, reasonAttribute.Value);
            }
        }

        [Fact]
        public async Task OptionalAttributesAreAbsentWhenNotProvided()
        {
            (CapturingLoggerProvider loggerProvider, ServiceProvider serviceProvider) = await CreateAndStartHostAsync();

            using (serviceProvider)
            {
                // No TargetingId, no Variant tag: mirrors an evaluation with no targeting context and no variant assigned.
                EmitFeatureFlagActivityEvent(new ActivityTagsCollection
                {
                    { "FeatureName", "TestFeature" },
                    { "Enabled", true },
                    { "VariantAssignmentReason", VariantAssignmentReason.None }
                });

                CapturedLogRecord record = Assert.Single(loggerProvider.Records, r => r.EventId.Name == FeatureEvaluationEventName);

                Assert.DoesNotContain(record.State, kvp => kvp.Key == "TargetingId");

                Assert.DoesNotContain(record.State, kvp => kvp.Key == "Variant");
            }
        }

        [Fact]
        public async Task ReservedCustomEventNameKeyCannotBeOverriddenByTelemetryMetadata()
        {
            (CapturingLoggerProvider loggerProvider, ServiceProvider serviceProvider) = await CreateAndStartHostAsync();

            using (serviceProvider)
            {
                // Simulates a feature flag's telemetry metadata containing a key that collides with
                // the reserved Azure Monitor custom event name attribute.
                EmitFeatureFlagActivityEvent(new ActivityTagsCollection
                {
                    { "FeatureName", "TestFeature" },
                    { "Enabled", true },
                    { AzureMonitorCustomEventNameKey, "override_attempt" },
                    { "CustomProperty", "custom_value" }
                });

                CapturedLogRecord record = Assert.Single(loggerProvider.Records, r => r.EventId.Name == FeatureEvaluationEventName);

                KeyValuePair<string, object> customEventNameAttribute = Assert.Single(record.State, kvp => kvp.Key == AzureMonitorCustomEventNameKey);

                Assert.Equal(FeatureEvaluationEventName, customEventNameAttribute.Value);

                KeyValuePair<string, object> customPropertyAttribute = Assert.Single(record.State, kvp => kvp.Key == "CustomProperty");

                Assert.Equal("custom_value", customPropertyAttribute.Value);

                // A warning should have been logged about the ignored, colliding key.
                Assert.Contains(loggerProvider.Records, r => r.LogLevel == LogLevel.Warning);
            }
        }

        [Fact]
        public async Task FullAttributeSetIncludesAllTypedValues()
        {
            (CapturingLoggerProvider loggerProvider, ServiceProvider serviceProvider) = await CreateAndStartHostAsync();

            using (serviceProvider)
            {
                EmitFeatureFlagActivityEvent(new ActivityTagsCollection
                {
                    { "FeatureName", "TestFeature" },
                    { "Enabled", true },
                    { "TargetingId", "test-user" },
                    { "Variant", "Big" },
                    { "VariantAssignmentReason", VariantAssignmentReason.Percentile },
                    { "VariantAssignmentPercentage", 42.5 },
                    { "Version", "1.0.0" },
                    { "ETag", "fake-etag" },
                    { "Label", "fake-label" }
                });

                CapturedLogRecord record = Assert.Single(loggerProvider.Records, r => r.EventId.Name == FeatureEvaluationEventName);

                Assert.Contains(record.State, kvp => kvp.Key == AzureMonitorCustomEventNameKey && (string)kvp.Value == FeatureEvaluationEventName);
                Assert.Contains(record.State, kvp => kvp.Key == "FeatureName" && (string)kvp.Value == "TestFeature");
                Assert.Contains(record.State, kvp => kvp.Key == "Enabled" && kvp.Value is bool b && b);
                Assert.Contains(record.State, kvp => kvp.Key == "TargetingId" && (string)kvp.Value == "test-user");
                Assert.Contains(record.State, kvp => kvp.Key == "Variant" && (string)kvp.Value == "Big");
                Assert.Contains(record.State, kvp => kvp.Key == "VariantAssignmentReason" && (string)kvp.Value == "Percentile");
                Assert.Contains(record.State, kvp => kvp.Key == "VariantAssignmentPercentage" && kvp.Value is double d && d == 42.5);
                Assert.Contains(record.State, kvp => kvp.Key == "Version" && (string)kvp.Value == "1.0.0");
                Assert.Contains(record.State, kvp => kvp.Key == "ETag" && (string)kvp.Value == "fake-etag");
                Assert.Contains(record.State, kvp => kvp.Key == "Label" && (string)kvp.Value == "fake-label");
            }
        }

        private static async Task<(CapturingLoggerProvider LoggerProvider, ServiceProvider ServiceProvider)> CreateAndStartHostAsync()
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

            return (loggerProvider, serviceProvider);
        }

        private static void EmitFeatureFlagActivityEvent(ActivityTagsCollection tags)
        {
            using var activitySource = new ActivitySource(FeatureManagementActivitySourceName);

            using Activity activity = activitySource.StartActivity("FeatureEvaluation");

            Assert.NotNull(activity);

            activity.AddEvent(new ActivityEvent("FeatureFlag", DateTimeOffset.UtcNow, tags));
        }
    }
}
