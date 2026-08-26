// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement.Telemetry.OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("appsettings.json");
    })
    .ConfigureServices((context, services) =>
    {
        string connectionString = context.Configuration["ApplicationInsights:ConnectionString"];

        //
        // Emits the OpenTelemetry log-based "FeatureEvaluation" custom event produced by
        // OpenTelemetryEventPublisher. This works independently of any TracerProvider.
        services.AddLogging(logging =>
        {
            logging.AddOpenTelemetry(options =>
            {
                options.AddConsoleExporter();

                if (!string.IsNullOrEmpty(connectionString))
                {
                    options.AddAzureMonitorLogExporter(o => o.ConnectionString = connectionString);
                }
            });
        });

        //
        // Only needed if the app also wants to export the raw Activity/span data emitted from
        // ActivitySource("Microsoft.FeatureManagement").
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddSource("Microsoft.FeatureManagement");

                tracing.AddConsoleExporter();

                if (!string.IsNullOrEmpty(connectionString))
                {
                    tracing.AddAzureMonitorTraceExporter(o => o.ConnectionString = connectionString);
                }
            });

        //
        // Enter feature management
        //
        // Enhance the application with feature management and wire up OpenTelemetry evaluation
        // event emission
        services.AddFeatureManagement()
            .AddOpenTelemetry();
    })
    .Build();

//
// Register TargetingActivityProcessor with the TracerProvider so it stamps TargetingId onto
// every Activity/span.
host.Services.GetRequiredService<TracerProvider>()
    .AddProcessor(host.Services.GetRequiredService<TargetingActivityProcessor>());

await host.StartAsync();

IVariantFeatureManager featureManager = host.Services.GetRequiredService<IVariantFeatureManager>();

var users = new[] { "Alice", "Bob", "Carol" };

//
// Mimic work items in a task-driven console application
foreach (string user in users)
{
    var targetingContext = new TargetingContext { UserId = user };

    Variant variant = await featureManager.GetVariantAsync("ImageRating", targetingContext);

    Console.WriteLine($"User '{user}' was assigned variant '{variant.Name}'.");
}

//
// Allow batched telemetry (the console exporter, and Azure Monitor if configured) to flush
await Task.Delay(TimeSpan.FromSeconds(2));

await host.StopAsync();
