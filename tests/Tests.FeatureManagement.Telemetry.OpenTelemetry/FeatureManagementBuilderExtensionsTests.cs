// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using System.Linq;
using Xunit;

namespace Tests.FeatureManagement.Telemetry.OpenTelemetry
{
    public class FeatureManagementBuilderExtensionsTests
    {
        [Fact]
        public void AddOpenTelemetryCalledTwiceRegistersHostedServiceOnlyOnce()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            IFeatureManagementBuilder builder = services.AddFeatureManagement();

            builder.AddOpenTelemetry();

            builder.AddOpenTelemetry();

            int hostedServiceDescriptorCount = services.Count(d => d.ServiceType == typeof(IHostedService));

            Assert.Equal(1, hostedServiceDescriptorCount);

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            Assert.Single(serviceProvider.GetServices<IHostedService>());
        }

        [Fact]
        public void AddOpenTelemetryReturnsSameBuilderInstance()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            IFeatureManagementBuilder builder = services.AddFeatureManagement();

            IFeatureManagementBuilder returnedBuilder = builder.AddOpenTelemetry();

            Assert.Same(builder, returnedBuilder);
        }
    }
}
