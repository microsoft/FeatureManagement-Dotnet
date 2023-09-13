// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.FeatureManagement.Telemetry.AppInsights
{
    /// <summary>
    /// Extensions used to add feature management publisher functionality.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an event publisher that publishes feature evaluation events to Application Insights.
        /// </summary>
        /// <param name="services">The service collection that feature management services are added to.</param>
        /// <returns>The <see cref="IServiceCollection"/> that was given as a parameter, with the publisher added.</returns>
        public static IServiceCollection AddFeatureManagementTelemetryPublisherAppInsights(this IServiceCollection services)
        {
            //
            // Add required services
            services.AddSingleton<ITelemetryPublisher, TelemetryPublisherAppInsights>();

            return services;
        }
    }
}
