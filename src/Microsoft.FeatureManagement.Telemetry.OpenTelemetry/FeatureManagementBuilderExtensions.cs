// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement.Telemetry.OpenTelemetry;
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
        /// Adds the <see cref="OpenTelemetryEventPublisher"/>, using <see cref="OpenTelemetryHostedService"/>, and the <see cref="TargetingActivityProcessor"/> and <see cref="TargetingLogProcessor"/> to the feature management builder.
        /// </summary>
        /// <remarks>
        /// <see cref="TargetingActivityProcessor"/> and <see cref="TargetingLogProcessor"/> are automatically registered with the app's <c>TracerProvider</c> and <c>LoggerProvider</c> respectively,
        /// so exported traces/spans and log-based telemetry (including custom events) are enriched with targeting information whenever a <c>TargetingId</c> is present in the current
        /// <see cref="System.Diagnostics.Activity"/>'s baggage. This method moves its processor registrations to the front of the service collection, so they always run before exporters
        /// configured elsewhere (for example via <c>ILoggingBuilder.AddOpenTelemetry(options =&gt; ...)</c> or <c>TracerProviderBuilder.AddConsoleExporter()</c>), regardless of whether this
        /// method is called before or after those exporters are configured.
        /// </remarks>
        /// <param name="builder">The feature management builder.</param>
        /// <returns>The feature management builder.</returns>
        public static IFeatureManagementBuilder AddOpenTelemetry(this IFeatureManagementBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (builder.Services == null)
            {
                throw new ArgumentException($"The provided builder's services must not be null.", nameof(builder));
            }

            builder.Services.AddSingleton<OpenTelemetryEventPublisher>();

            builder.Services.AddSingleton<TargetingActivityProcessor>();

            builder.Services.AddSingleton<TargetingLogProcessor>();

            // Automatically wire TargetingActivityProcessor ahead of any span exporters so every
            // exported Activity/span is enriched with TargetingId.
            builder.Services.ConfigureOpenTelemetryTracerProvider((serviceProvider, tracerProviderBuilder) =>
                tracerProviderBuilder.AddProcessor(serviceProvider.GetRequiredService<TargetingActivityProcessor>()));

            MoveLastDescriptorToFront(builder.Services, "OpenTelemetry.Trace.IConfigureTracerProviderBuilder");

            // Automatically wire TargetingLogProcessor ahead of any log exporters so every exported
            // log-based telemetry item (not just the FeatureEvaluation event) is enriched with TargetingId.
            builder.Services.ConfigureOpenTelemetryLoggerProvider((serviceProvider, loggerProviderBuilder) =>
                loggerProviderBuilder.AddProcessor(serviceProvider.GetRequiredService<TargetingLogProcessor>()));

            MoveLastDescriptorToFront(builder.Services, "OpenTelemetry.Logs.IConfigureLoggerProviderBuilder");

            if (!builder.Services.Any((ServiceDescriptor d) => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(OpenTelemetryHostedService)))
            {
                builder.Services.Insert(0, ServiceDescriptor.Singleton<IHostedService, OpenTelemetryHostedService>());
            }

            return builder;
        }

        //
        // OpenTelemetry's TracerProviderBuilder/LoggerProviderBuilder configuration callbacks (added via
        // ConfigureOpenTelemetryTracerProvider/ConfigureOpenTelemetryLoggerProvider) run in service registration
        // order. Moving the callback we just added to the front of the service collection guarantees our
        // processors are added to the provider - and therefore run - before any processors/exporters registered
        // elsewhere, regardless of whether this method is called before or after those exporters are configured.
        private static void MoveLastDescriptorToFront(IServiceCollection services, string configureBuilderTypeFullName)
        {
            ServiceDescriptor descriptor = services.LastOrDefault(d => d.ServiceType.FullName == configureBuilderTypeFullName);

            if (descriptor != null)
            {
                services.Remove(descriptor);

                services.Insert(0, descriptor);
            }
        }
    }
}
