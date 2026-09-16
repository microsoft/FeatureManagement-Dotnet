// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement.Telemetry.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Extensions used to integrate feature management with OpenTelemetry.
    /// </summary>
    public static class OpenTelemetryBuilderExtensions
    {
        /// <summary>
        /// Adds feature evaluation event publishing and targeting enrichment to OpenTelemetry.
        /// </summary>
        /// <remarks>
        /// Automatically registers <see cref="TargetingActivityProcessor"/> and <see cref="TargetingLogProcessor"/>
        /// to enrich spans and logs with <c>TargetingId</c> from activity baggage.
        /// Call this method before configuring exporters, including <c>UseAzureMonitor()</c>
        /// or <c>UseAzureMonitorExporter()</c>.
        /// Repeated calls for the same service collection have no effect.
        /// </remarks>
        /// <param name="builder">The OpenTelemetry builder.</param>
        /// <returns>The supplied OpenTelemetry builder.</returns>
        public static IOpenTelemetryBuilder WithFeatureManagement(this IOpenTelemetryBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (builder.Services == null)
            {
                throw new ArgumentException($"The provided builder's services must not be null.", nameof(builder));
            }

            if (builder.Services.Any(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(FeatureEvaluationEventPublisherHostedService)))
            {
                return builder;
            }

            builder.Services.TryAddSingleton<FeatureEvaluationEventPublisher>();

            builder.Services.TryAddSingleton<TargetingActivityProcessor>();

            builder.Services.TryAddSingleton<TargetingLogProcessor>();

            builder.Services.ConfigureOpenTelemetryTracerProvider((serviceProvider, tracerProviderBuilder) =>
                tracerProviderBuilder.AddProcessor(serviceProvider.GetRequiredService<TargetingActivityProcessor>()));

            builder.Services.ConfigureOpenTelemetryLoggerProvider((serviceProvider, loggerProviderBuilder) =>
                loggerProviderBuilder.AddProcessor(serviceProvider.GetRequiredService<TargetingLogProcessor>()));

            builder.Services.Insert(0, ServiceDescriptor.Singleton<IHostedService, FeatureEvaluationEventPublisherHostedService>());

            return builder;
        }
    }
}
