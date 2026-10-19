// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Telemetry.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using System;
using System.Linq;
using Xunit;

namespace Tests.FeatureManagement.Telemetry.OpenTelemetry
{
    public class OpenTelemetryBuilderExtensionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AddFeatureManagementProcessorsCalledTwiceDoesNotDuplicateRegistrations(bool useNewBuilder)
        {
            var services = new ServiceCollection();

            services.AddLogging();

            var builder = new TestOpenTelemetryBuilder(services);

            builder.AddFeatureManagementProcessors();

            ServiceDescriptor[] descriptors = services.ToArray();

            if (useNewBuilder)
            {
                builder = new TestOpenTelemetryBuilder(services);
            }

            Assert.Same(builder, builder.AddFeatureManagementProcessors());

            Assert.Equal(descriptors, services.ToArray());
            Assert.Single(services, d => d.ServiceType == typeof(TargetingActivityProcessor));
            Assert.Single(services, d => d.ServiceType == typeof(TargetingLogProcessor));
            Assert.Equal(typeof(IHostedService), services[0].ServiceType);

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            Assert.Single(serviceProvider.GetServices<IHostedService>());
        }

        [Fact]
        public void AddFeatureManagementProcessorsReturnsSameBuilderInstance()
        {
            var builder = new TestOpenTelemetryBuilder(new ServiceCollection());

            IOpenTelemetryBuilder returnedBuilder = builder.AddFeatureManagementProcessors();

            Assert.Same(builder, returnedBuilder);
        }

        [Fact]
        public void AddFeatureManagementProcessorsRejectsNullBuilder()
        {
            IOpenTelemetryBuilder builder = null;

            Assert.Throws<ArgumentNullException>("builder", () => builder.AddFeatureManagementProcessors());
        }

        [Fact]
        public void AddFeatureManagementProcessorsRejectsNullServices()
        {
            var builder = new TestOpenTelemetryBuilder(null);

            Assert.Throws<ArgumentException>("builder", () => builder.AddFeatureManagementProcessors());
        }

        [Fact]
        public void AddFeatureManagementProcessorsDoesNotEnableProviders()
        {
            var services = new ServiceCollection();

            services.AddOpenTelemetry().AddFeatureManagementProcessors();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            Assert.Null(serviceProvider.GetService<TracerProvider>());
            Assert.Null(serviceProvider.GetService<LoggerProvider>());
        }

        [Fact]
        public void AddFeatureManagementProcessorsPreservesExistingDescriptors()
        {
            var services = new ServiceCollection();

            OpenTelemetryBuilder builder = services.AddOpenTelemetry()
                .WithTracing(tracing => tracing.AddSource("ExistingSource"))
                .WithLogging();

            ServiceDescriptor[] descriptors = services.ToArray();

            builder.AddFeatureManagementProcessors();

            // Only our hosted service is prepended; existing provider configuration stays in place.
            Assert.Equal(typeof(IHostedService), services[0].ServiceType);
            Assert.Equal(descriptors, services.Skip(1).Take(descriptors.Length).ToArray());
        }

        private sealed class TestOpenTelemetryBuilder : IOpenTelemetryBuilder
        {
            public IServiceCollection Services { get; }

            public TestOpenTelemetryBuilder(IServiceCollection services)
            {
                Services = services;
            }
        }
    }
}
