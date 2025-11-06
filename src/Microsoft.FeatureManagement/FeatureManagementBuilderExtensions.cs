// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FeatureManagement.FeatureFilters;
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
        /// <exception cref="InvalidOperationException">Thrown if a variant service of the type has already been added.</exception>
        public static IFeatureManagementBuilder WithVariantService<TService>(this IFeatureManagementBuilder builder, string featureName) where TService : class
        {
            if (string.IsNullOrEmpty(featureName))
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IVariantServiceProvider<TService>)))
            {
                throw new InvalidOperationException($"A variant service of {typeof(TService).FullName} has already been added.");
            }

            // Capture the service descriptors before the service provider is built
            // This allows us to create factories without instantiating services eagerly
            List<ServiceDescriptor> serviceDescriptors = builder.Services
                .Where(descriptor => descriptor.ServiceType == typeof(TService))
                .ToList();

            if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IFeatureManager) && descriptor.Lifetime == ServiceLifetime.Scoped))
            {
                builder.Services.AddScoped<IVariantServiceProvider<TService>>(sp => new VariantServiceProvider<TService>(
                    featureName,
                    sp.GetRequiredService<IVariantFeatureManager>(),
                    CreateServiceFactories<TService>(sp, serviceDescriptors)));
            }
            else
            {
                builder.Services.AddSingleton<IVariantServiceProvider<TService>>(sp => new VariantServiceProvider<TService>(
                    featureName,
                    sp.GetRequiredService<IVariantFeatureManager>(),
                    CreateServiceFactories<TService>(sp, serviceDescriptors)));
            }

            return builder;
        }

        /// <summary>
        /// Creates factory delegates for all registered implementations of TService.
        /// This enables lazy instantiation - services are only created when actually needed.
        /// </summary>
        private static IEnumerable<VariantServiceFactory<TService>> CreateServiceFactories<TService>(
            IServiceProvider serviceProvider,
            List<ServiceDescriptor> serviceDescriptors) where TService : class
        {
            var factories = new List<VariantServiceFactory<TService>>();

            foreach (ServiceDescriptor descriptor in serviceDescriptors)
            {
                Type implementationType = GetImplementationType(descriptor);

                // Create a lazy factory for each service
                // The factory creates a singleton instance that is only instantiated when first invoked
                var lazyService = new Lazy<TService>(() => CreateServiceFromDescriptor<TService>(serviceProvider, descriptor));

                // For factory-based registrations, implementationType will be null
                // The VariantServiceFactory will lazily determine the type by invoking the factory
                factories.Add(new VariantServiceFactory<TService>(
                    implementationType,
                    () => lazyService.Value));
            }

            return factories;
        }

        /// <summary>
        /// Creates a service instance from a service descriptor.
        /// </summary>
        private static TService CreateServiceFromDescriptor<TService>(IServiceProvider serviceProvider, ServiceDescriptor descriptor) where TService : class
        {
            if (descriptor.ImplementationInstance != null)
            {
                return descriptor.ImplementationInstance as TService;
            }
            else if (descriptor.ImplementationFactory != null)
            {
                return descriptor.ImplementationFactory(serviceProvider) as TService;
            }
            else if (descriptor.ImplementationType != null)
            {
                // Use ActivatorUtilities to create the instance with dependency injection
                return ActivatorUtilities.CreateInstance(serviceProvider, descriptor.ImplementationType) as TService;
            }

            return null;
        }

        /// <summary>
        /// Gets the implementation type from a service descriptor.
        /// Returns null for factory-based registrations where the type cannot be determined without invocation.
        /// </summary>
        private static Type GetImplementationType(ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationType != null)
            {
                return descriptor.ImplementationType;
            }
            else if (descriptor.ImplementationInstance != null)
            {
                return descriptor.ImplementationInstance.GetType();
            }
            else if (descriptor.ImplementationFactory != null)
            {
                // For factory-based registrations, we can't determine the type without invoking the factory
                // Return null and let VariantServiceFactory lazily determine it
                return null;
            }

            return null;
        }
    }
}
