// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement.FeatureFilters;

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
                Services.AddSingleton(serviceType, implementationType);
            }

            return this;
        }

        public IFeatureManagementBuilder AddSessionManager<T>() where T : ISessionManager
        {
            Services.AddSingleton(typeof(ISessionManager), typeof(T));

            return this;
        }

        public IFeatureManagementBuilder WithTargeting<T>() where T : ITargetingContextAccessor
        {
            Type serviceType = typeof(ITargetingContextAccessor);

            Type implementationType = typeof(T);

            if (!Services.Any(descriptor => descriptor.ServiceType == serviceType && descriptor.ImplementationType != implementationType))
            {
                Services.AddSingleton(serviceType, implementationType);
            }

            AddFeatureFilter<TargetingFilter>();

            return this;
        }
    }
}
