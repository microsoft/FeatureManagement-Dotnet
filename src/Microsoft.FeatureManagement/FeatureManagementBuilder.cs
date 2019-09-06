// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
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

            if (!Services.Any(descriptor => descriptor.ServiceType == serviceType && descriptor.ImplementationType == implementationType))
            {
                Services.AddSingleton(typeof(IFeatureFilterMetadata), typeof(T));
            }

            return this;
        }

        public IFeatureManagementBuilder AddSessionManager<T>() where T : ISessionManager
        {
            Services.AddSingleton(typeof(ISessionManager), typeof(T));

            return this;
        }
    }
}
