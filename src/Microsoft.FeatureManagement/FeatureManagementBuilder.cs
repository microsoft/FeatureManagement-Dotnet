// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a way to customize feature management.
    /// </summary>
    class FeatureManagementBuilder : IFeatureManagementBuilder
    {
        public FeatureManagementBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public IServiceCollection Services { get; }

        public IFeatureManagementBuilder AddFeatureFilter<T>() where T : IFeatureFilterMetadata
        {
            Type serviceType = typeof(IFeatureFilterMetadata);

            Type implementationType = typeof(T);

            IEnumerable<Type> featureFilterImplementations = implementationType.GetInterfaces()
                .Where(i => i == typeof(IFeatureFilter) || 
                            (i.IsGenericType && i.GetGenericTypeDefinition().IsAssignableFrom(typeof(IContextualFeatureFilter<>))));

            if (featureFilterImplementations.Count() > 1)
            {
                throw new ArgumentException($"A single feature filter cannot implement more than one feature filter interface.", nameof(T));
            }

            if (!Services.Any(descriptor => descriptor.ServiceType == serviceType && descriptor.ImplementationType == implementationType))
            {
                //
                // Register the feature filter with the same lifetime as the feature manager
                if (Services.Any(descriptor => descriptor.ServiceType == typeof(IFeatureManager) && descriptor.Lifetime == ServiceLifetime.Scoped))
                {
                    Services.AddScoped(serviceType, implementationType);
                }
                else
                {
                    Services.AddSingleton(serviceType, implementationType);
                }
            }

            return this;
        }

        public IFeatureManagementBuilder AddSessionManager<T>() where T : ISessionManager
        {
            //
            // Register the session manager with the same lifetime as the feature manager
            if (Services.Any(descriptor => descriptor.ServiceType == typeof(IFeatureManager) && descriptor.Lifetime == ServiceLifetime.Scoped))
            {
                Services.AddScoped(typeof(ISessionManager), typeof(T));
            }
            else
            {
                Services.AddSingleton(typeof(ISessionManager), typeof(T));
            }

            return this;
        }
    }
}
