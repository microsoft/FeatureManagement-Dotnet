// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a way to customize feature management.
    /// </summary>
    class FeatureManagementBuilder : IFeatureManagementBuilder
    {
        private readonly bool IsFeatureManagerScoped;

        public FeatureManagementBuilder(
            IServiceCollection services,
            bool isFeatureManagerScoped)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            IsFeatureManagerScoped = isFeatureManagerScoped;
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
                if (!IsFeatureManagerScoped)
                {
                    Services.AddSingleton(typeof(IFeatureFilterMetadata), typeof(T));
                }
                else
                {
                    Services.AddScoped(typeof(IFeatureFilterMetadata), typeof(T));
                }
            }

            return this;
        }

        public IFeatureManagementBuilder AddSessionManager<T>() where T : ISessionManager
        {
            if (!IsFeatureManagerScoped)
            {
                Services.AddSingleton(typeof(ISessionManager), typeof(T));
            }
            else
            {
                Services.AddScoped(typeof(ISessionManager), typeof(T));
            }

            return this;
        }
    }
}
