// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Telemetry.OpenTelemetry;
using OpenTelemetry.Logs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
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

        [Fact]
        public async Task ExportedLogRecordIsEnrichedWithTargetingIdWhenProcessorIsAutomaticallyWired()
        {
            var exportedLogRecords = new List<LogRecord>();

            var services = new ServiceCollection();

            services.AddFeatureManagement().AddOpenTelemetry();

            services.AddLogging(logging =>
            {
                logging.AddOpenTelemetry(options =>
                {
                    options.AddInMemoryExporter(exportedLogRecords);
                });
            });

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            using (serviceProvider)
            {
                foreach (IHostedService hostedService in serviceProvider.GetServices<IHostedService>())
                {
                    await hostedService.StartAsync(default);
                }

                ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Test");

                using var activity = new Activity("WithBaggage");

                activity.AddBaggage(TargetingIdKey, "Bob");

                activity.Start();

                logger.LogInformation("checkout");

                activity.Stop();

                LogRecord exportedRecord = Assert.Single(exportedLogRecords);

                Assert.Contains(exportedRecord.Attributes, kvp => kvp.Key == TargetingIdKey && (string)kvp.Value == "Bob");
            }
        }

        [Fact]
        public async Task ExportedLogRecordIsEnrichedWithTargetingIdWhenAddOpenTelemetryIsCalledAfterLogExportersAreConfigured()
        {
            var exportedLogRecords = new List<LogRecord>();

            var services = new ServiceCollection();

            // Configure the log exporter BEFORE AddFeatureManagement().AddOpenTelemetry() (the
            // opposite order from the test above) to prove that TargetingLogProcessor still runs
            // ahead of the exporter regardless of call order.
            services.AddLogging(logging =>
            {
                logging.AddOpenTelemetry(options =>
                {
                    options.AddInMemoryExporter(exportedLogRecords);
                });
            });

            services.AddFeatureManagement().AddOpenTelemetry();

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            using (serviceProvider)
            {
                foreach (IHostedService hostedService in serviceProvider.GetServices<IHostedService>())
                {
                    await hostedService.StartAsync(default);
                }

                ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Test");

                using var activity = new Activity("WithBaggage");

                activity.AddBaggage(TargetingIdKey, "Dana");

                activity.Start();

                logger.LogInformation("checkout");

                activity.Stop();

                LogRecord exportedRecord = Assert.Single(exportedLogRecords);

                Assert.Contains(exportedRecord.Attributes, kvp => kvp.Key == TargetingIdKey && (string)kvp.Value == "Dana");
            }
        }

        private static LogRecord CaptureLogRecord(string targetingId, Action<ILogger> logAction)
        {
            var exportedLogRecords = new List<LogRecord>();

            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(options =>
                {
                    // Register TargetingLogProcessor ahead of the in-memory exporter, mirroring
                    // how FeatureManagementBuilderExtensions.AddOpenTelemetry wires it in apps.
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
