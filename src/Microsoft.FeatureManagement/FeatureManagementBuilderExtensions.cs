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
using System.Reflection;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Extensions used to add feature management functionality.
    /// </summary>
    public static class FeatureManagementBuilderExtensions
    {
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

        /// <summary>
        /// Adds a <see cref="VariantServiceProvider{TService}"/> to the feature management system.
        /// </summary>
        /// <param name="builder">The <see cref="IFeatureManagementBuilder"/> used to customize feature management functionality.</param>
        /// <param name="featureName">The feature flag that should be used to determine which variant of the service should be used. The <see cref="VariantServiceProvider{TService}"/> will return different implementations of TService according to the assigned variant.</param>
        /// <returns>A <see cref="IFeatureManagementBuilder"/> that can be used to customize feature management functionality.</returns>
        /// <exception cref="ArgumentNullException">Thrown if feature name parameter is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the variant service of the type has been added.</exception>
        public static IFeatureManagementBuilder WithVariantService<TService>(this IFeatureManagementBuilder builder, string featureName) where TService : class
        {
            if (string.IsNullOrEmpty(featureName))
            {
                throw new ArgumentNullException(nameof(featureName));
            }
            
            if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IVariantServiceProvider<TService>)))
            {
                throw new InvalidOperationException($"Variant services of {typeof(TService)} has been added.");
            }

            if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IFeatureManager) && descriptor.Lifetime == ServiceLifetime.Scoped))
            {
                builder.Services.AddScoped<IVariantServiceProvider<TService>>(sp => new VariantServiceProvider<TService>(
                    sp.GetRequiredService<IEnumerable<TService>>(),
                    sp.GetRequiredService<IVariantFeatureManager>())
                {
                    FeatureName = featureName,
                });
            }
            else
            {
                builder.Services.AddSingleton<IVariantServiceProvider<TService>>(sp => new VariantServiceProvider<TService>(
                    sp.GetRequiredService<IEnumerable<TService>>(),
                    sp.GetRequiredService<IVariantFeatureManager>())
                {
                    FeatureName = featureName,
                });
            }

            return builder;
        }

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
    }
}
