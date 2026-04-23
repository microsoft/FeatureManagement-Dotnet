// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit;

namespace Tests.FeatureManagement.Telemetry.AzureMonitor
{
    public class AzureMonitorTelemetryTests
    {
        [Fact]
        public async Task LogsFeatureEvaluationWithAzureMonitor()
        {
            // Arrange
            var logMessages = new ConcurrentBag<LogEntry>();

            var configValues = new Dictionary<string, string>
            {
                ["feature_management:feature_flags:0:id"] = "TestFeature",
                ["feature_management:feature_flags:0:enabled"] = "true",
                ["feature_management:feature_flags:0:telemetry:enabled"] = "true"
            };

            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(configValues);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(new TestLoggerProvider(logMessages));
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .ConfigureServices(services =>
                {
                    services.AddFeatureManagement()
                        .AddAzureMonitorTelemetry();
                });

            var host = hostBuilder.Build();

            // Start the host to initialize the hosted service and event publisher
            await host.StartAsync();

            // Act
            // Start an activity to enable telemetry publishing
            using Activity testActivity = new Activity("TestActivity").Start();

            var featureManager = host.Services.GetRequiredService<IFeatureManager>();
            bool isEnabled = await featureManager.IsEnabledAsync("TestFeature");

            // Wait a moment for the activity to be processed
            await Task.Delay(500);

            // Assert
            Assert.True(isEnabled);

            // Debug: Check if ANY logs were captured
            Console.WriteLine($"Total logs captured: {logMessages.Count}");
            foreach (var log in logMessages)
            {
                Console.WriteLine($"  [{log.LogLevel}] {log.Category}: {log.Message}");
            }

            Assert.NotEmpty(logMessages);

            var featureEvaluationLogs = logMessages
                .Where(log => log.Message.Contains("FeatureEvaluation"))
                .ToList();

            // If no FeatureEvaluation logs, check what we did get
            if (featureEvaluationLogs.Count == 0)
            {
                var allLogs = string.Join("\n", logMessages.Select(l => $"{l.Category}: {l.Message}"));
                throw new Exception($"No FeatureEvaluation logs found. All logs:\n{allLogs}");
            }

            Assert.NotEmpty(featureEvaluationLogs);

            var evaluationLog = featureEvaluationLogs.First();
            Assert.Equal(LogLevel.Information, evaluationLog.LogLevel);
            Assert.Contains("FeatureEvaluation", evaluationLog.Message);

            await host.StopAsync();
        }

        [Fact]
        public async Task AzureMonitorHostedServiceStartsSuccessfully()
        {
            // Arrange
            var configValues = new Dictionary<string, string>
            {
                ["feature_management:feature_flags:0:id"] = "TestFeature",
                ["feature_management:feature_flags:0:enabled"] = "true",
                ["feature_management:feature_flags:0:telemetry:enabled"] = "true"
            };

            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(configValues);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                })
                .ConfigureServices(services =>
                {
                    services.AddFeatureManagement()
                        .AddAzureMonitorTelemetry();
                });

            var host = hostBuilder.Build();

            // Act
            await host.StartAsync();

            // Assert - verify hosted service is registered and started
            var hostedServices = host.Services.GetServices<IHostedService>().ToList();
            Assert.NotEmpty(hostedServices);

            // Verify we can use feature management
            var featureManager = host.Services.GetRequiredService<IFeatureManager>();
            bool isEnabled = await featureManager.IsEnabledAsync("TestFeature");
            Assert.True(isEnabled);

            await host.StopAsync();
        }
    }

    // Test logger infrastructure
    public class LogEntry
    {
        public LogLevel LogLevel { get; set; }
        public string Message { get; set; }
        public string Category { get; set; }
    }

    public class TestLogger : ILogger
    {
        private readonly ConcurrentBag<LogEntry> _logMessages;
        private readonly string _category;

        public TestLogger(ConcurrentBag<LogEntry> logMessages, string category)
        {
            _logMessages = logMessages;
            _category = category;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);

            _logMessages.Add(new LogEntry
            {
                LogLevel = logLevel,
                Message = message,
                Category = _category
            });
        }
    }

    public class TestLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentBag<LogEntry> _logMessages;

        public TestLoggerProvider(ConcurrentBag<LogEntry> logMessages)
        {
            _logMessages = logMessages;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(_logMessages, categoryName);
        }

        public void Dispose()
        {
        }
    }
}
