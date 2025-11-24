// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement.Telemetry.AzureMonitor;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Extensions used to add feature management functionality.
    /// </summary>
    public static class FeatureManagementBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="AzureMonitorEventPublisher"/> using <see cref="AzureMonitorHostedService"/> to the feature management builder.
        /// </summary>
        /// <param name="builder">The feature management builder.</param>
        /// <returns>The feature management builder.</returns>
        public static IFeatureManagementBuilder AddAzureMonitorTelemetry(this IFeatureManagementBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (builder.Services == null)
            {
                throw new ArgumentException($"The provided builder's services must not be null.", nameof(builder));
            }

            builder.Services.AddSingleton<AzureMonitorEventPublisher>();

            if (!builder.Services.Any((ServiceDescriptor d) => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(AzureMonitorHostedService)))
            {
                builder.Services.Insert(0, ServiceDescriptor.Singleton<IHostedService, AzureMonitorHostedService>());
            }

            builder.Services.ConfigureOpenTelemetryTracerProvider(builder => builder.AddProcessor(new TargetingActivityProcessor()));
            builder.Services.ConfigureOpenTelemetryLoggerProvider(builder => builder.AddProcessor(new TargetingLogProcessor()));

            return builder;
        }
    }
}
