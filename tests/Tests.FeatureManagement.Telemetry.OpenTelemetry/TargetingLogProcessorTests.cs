// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Telemetry.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Logs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace Tests.FeatureManagement.Telemetry.OpenTelemetry
{
    public class TargetingLogProcessorTests
    {
        private const string TargetingIdKey = "TargetingId";

        [Fact]
        public void NullLogRecordIsNoOp()
        {
            var processor = new TargetingLogProcessor();

            Exception exception = Record.Exception(() => processor.OnEnd(null));

            Assert.Null(exception);
        }

        [Fact]
        public void BaggagePresentAddsTargetingIdAttribute()
        {
            LogRecord exportedRecord = CaptureLogRecord(
                targetingId: "Alice",
                logAction: logger => logger.LogInformation("checkout"));

            Assert.Contains(exportedRecord.Attributes, kvp => kvp.Key == TargetingIdKey && (string)kvp.Value == "Alice");
        }

        [Fact]
        public void BaggageAbsentDoesNotAddTargetingIdAttribute()
        {
            LogRecord exportedRecord = CaptureLogRecord(
                targetingId: null,
                logAction: logger => logger.LogInformation("checkout"));

            Assert.DoesNotContain(exportedRecord.Attributes ?? Array.Empty<KeyValuePair<string, object>>(), kvp => kvp.Key == TargetingIdKey);
        }

        [Fact]
        public void EmptyBaggageValueDoesNotAddTargetingIdAttribute()
        {
            LogRecord exportedRecord = CaptureLogRecord(
                targetingId: string.Empty,
                logAction: logger => logger.LogInformation("checkout"));

            Assert.DoesNotContain(exportedRecord.Attributes ?? Array.Empty<KeyValuePair<string, object>>(), kvp => kvp.Key == TargetingIdKey);
        }

        [Fact]
        public void ExistingTargetingIdAttributeIsNotOverwritten()
        {
            LogRecord exportedRecord = CaptureLogRecord(
                targetingId: "Alice",
                logAction: logger => logger.LogInformation("checkout {TargetingId}", "ExplicitValue"));

            KeyValuePair<string, object> tag = Assert.Single(exportedRecord.Attributes, kvp => kvp.Key == TargetingIdKey);

            Assert.Equal("ExplicitValue", tag.Value);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void AutomaticallyWiredProcessorRespectsExporterRegistrationOrder(bool integrationFirst, bool useLoggingBuilder)
        {
            var exporter = new CapturingExporter<LogRecord>(record => record.Attributes);

            var services = new ServiceCollection();

            services.AddFeatureManagement();

            OpenTelemetryBuilder builder = services.AddOpenTelemetry();

            if (integrationFirst)
            {
                builder.WithFeatureManagement();
            }

            if (useLoggingBuilder)
            {
                services.AddLogging(logging => logging.AddOpenTelemetry(options =>
                    options.AddProcessor(new SimpleLogRecordExportProcessor(exporter))));
            }
            else
            {
                builder.WithLogging(logging =>
                    logging.AddProcessor(new SimpleLogRecordExportProcessor(exporter)));
            }

            if (!integrationFirst)
            {
                builder.WithFeatureManagement();
            }

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Test");

            using var activity = new Activity("WithBaggage");

            activity.Start();

            activity.AddBaggage(TargetingIdKey, "Bob");

            logger.LogInformation("checkout");

            activity.Stop();

            IReadOnlyList<KeyValuePair<string, object>> attributes = Assert.Single(exporter.Records);

            if (integrationFirst)
            {
                Assert.Contains(attributes, kvp => kvp.Key == TargetingIdKey && (string)kvp.Value == "Bob");
            }
            else
            {
                Assert.DoesNotContain(attributes, kvp => kvp.Key == TargetingIdKey);
            }
        }

        private static LogRecord CaptureLogRecord(string targetingId, Action<ILogger> logAction)
        {
            var exportedLogRecords = new List<LogRecord>();

            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(options =>
                {
                    options.AddProcessor(new TargetingLogProcessor());

                    options.AddInMemoryExporter(exportedLogRecords);
                });
            });

            ILogger logger = loggerFactory.CreateLogger("Test");

            Activity activity = null;

            if (targetingId != null)
            {
                activity = new Activity("WithBaggage");

                activity.AddBaggage(TargetingIdKey, targetingId);

                activity.Start();
            }

            try
            {
                logAction(logger);
            }
            finally
            {
                activity?.Stop();
            }

            return Assert.Single(exportedLogRecords);
        }
    }
}
