// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Extensions used to add feature management functionality.
    /// </summary>
    public static class FeatureManagementBuilderExtensions
    {
        /// <summary>
        /// Adds a telemetry publisher to the feature management system.
        /// </summary>
        /// <param name="builder">The <see cref="IFeatureManagementBuilder"/> used to customize feature management functionality.</param>
        /// <returns>A <see cref="IFeatureManagementBuilder"/> that can be used to customize feature management functionality.</returns>
        public static IFeatureManagementBuilder AddTelemetryPublisher<T>(this IFeatureManagementBuilder builder) where T : ITelemetryPublisher
        {
            builder.AddTelemetryPublisher(sp => ActivatorUtilities.CreateInstance(sp, typeof(T)) as ITelemetryPublisher);

            return builder;
        }

        private static IFeatureManagementBuilder AddTelemetryPublisher(this IFeatureManagementBuilder builder, Func<IServiceProvider, ITelemetryPublisher> factory)
        {
            builder.Services.Configure<FeatureManagementOptions>(options =>
            {
                if (options.TelemetryPublisherFactories == null)
                {
                    options.TelemetryPublisherFactories = new List<Func<IServiceProvider, ITelemetryPublisher>>();
                }

                options.TelemetryPublisherFactories.Add(factory);
            });

            return builder;
        }

        /// <summary>
        /// Adds an <see cref="ITargetingContextAccessor"/> to be used for targeting and registers the targeting filter to the feature management system.
        /// </summary>
        /// <param name="builder">The <see cref="IFeatureManagementBuilder"/> used to customize feature management functionality.</param>
        /// <returns>A <see cref="IFeatureManagementBuilder"/> that can be used to customize feature management functionality.</returns>
        public static IFeatureManagementBuilder WithTargeting<T>(this IFeatureManagementBuilder builder) where T : ITargetingContextAccessor
        {
            //
            // Register the targeting context accessor with the same lifetime as the feature manager
            if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IFeatureManager) && descriptor.Lifetime == ServiceLifetime.Scoped))
            {
                builder.Services.TryAddScoped(typeof(ITargetingContextAccessor), typeof(T));
            }
            else
            {
                builder.Services.TryAddSingleton(typeof(ITargetingContextAccessor), typeof(T));
            }

            builder.AddFeatureFilter<TargetingFilter>();

            return builder;
        }
    }
}
