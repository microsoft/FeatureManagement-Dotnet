// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.FeatureManagement
{
    public class VariantServiceProviderTest
    {
        [Fact]
        public async Task VariantBasedInjection()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            IServiceCollection services = new ServiceCollection();

            services.AddSingleton<IAlgorithm, AlgorithmBeta>();
            services.AddSingleton<IAlgorithm, AlgorithmSigma>();
            services.AddSingleton<IAlgorithm>(sp => new AlgorithmOmega("OMEGA"));

            services.AddSingleton(configuration)
                .AddFeatureManagement()
                .AddFeatureFilter<TargetingFilter>()
                .WithVariantService<IAlgorithm>(Features.VariantImplementationFeature);

            var targetingContextAccessor = new OnDemandTargetingContextAccessor();

            services.AddSingleton<ITargetingContextAccessor>(targetingContextAccessor);

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IVariantFeatureManager featureManager = serviceProvider.GetRequiredService<IVariantFeatureManager>();

            IVariantServiceProvider<IAlgorithm> featuredAlgorithm = serviceProvider.GetRequiredService<IVariantServiceProvider<IAlgorithm>>();

            targetingContextAccessor.Current = new TargetingContext
            {
                UserId = "Guest"
            };

            IAlgorithm algorithm = await featuredAlgorithm.GetServiceAsync(CancellationToken.None);

            Assert.Null(algorithm);

            targetingContextAccessor.Current = new TargetingContext
            {
                UserId = "UserSigma"
            };

            algorithm = await featuredAlgorithm.GetServiceAsync(CancellationToken.None);

            Assert.Null(algorithm);

            targetingContextAccessor.Current = new TargetingContext
            {
                UserId = "UserBeta"
            };

            algorithm = await featuredAlgorithm.GetServiceAsync(CancellationToken.None);

            Assert.NotNull(algorithm);
            Assert.Equal("Beta", algorithm.Style);

            targetingContextAccessor.Current = new TargetingContext
            {
                UserId = "UserOmega"
            };

            algorithm = await featuredAlgorithm.GetServiceAsync(CancellationToken.None);

            Assert.NotNull(algorithm);
            Assert.Equal("OMEGA", algorithm.Style);

            services = new ServiceCollection();

            Assert.Throws<InvalidOperationException>(() =>
                {
                    services.AddFeatureManagement()
                        .WithVariantService<IAlgorithm>("DummyFeature1")
                        .WithVariantService<IAlgorithm>("DummyFeature2");
                }
            );
        }

        [Fact]
        public async Task VariantServiceProviderResolvesKeyedService()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            IServiceCollection services = new ServiceCollection();

            services.AddKeyedSingleton<IAlgorithm, AlgorithmBeta>("AlgorithmBeta");
            services.AddKeyedSingleton<IAlgorithm, AlgorithmSigma>("Sigma");
            services.AddKeyedSingleton<IAlgorithm>("Omega", (sp, _) => new AlgorithmOmega("OMEGA"));

            services.AddSingleton(configuration)
                .AddFeatureManagement()
                .AddFeatureFilter<TargetingFilter>()
                .WithVariantService<IAlgorithm>(Features.VariantImplementationFeature);

            var targetingContextAccessor = new OnDemandTargetingContextAccessor();

            services.AddSingleton<ITargetingContextAccessor>(targetingContextAccessor);

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IVariantServiceProvider<IAlgorithm> featuredAlgorithm = serviceProvider.GetRequiredService<IVariantServiceProvider<IAlgorithm>>();

            targetingContextAccessor.Current = new TargetingContext { UserId = "UserBeta" };
            IAlgorithm algorithm = await featuredAlgorithm.GetServiceAsync(CancellationToken.None);
            Assert.NotNull(algorithm);
            Assert.Equal("Beta", algorithm.Style);

            targetingContextAccessor.Current = new TargetingContext { UserId = "UserSigma" };
            algorithm = await featuredAlgorithm.GetServiceAsync(CancellationToken.None);
            Assert.NotNull(algorithm);
            Assert.Equal("Sigma", algorithm.Style);

            targetingContextAccessor.Current = new TargetingContext { UserId = "UserOmega" };
            algorithm = await featuredAlgorithm.GetServiceAsync(CancellationToken.None);
            Assert.NotNull(algorithm);
            Assert.Equal("OMEGA", algorithm.Style);
        }

        [Fact]
        public async Task VariantServiceProviderKeyedServiceIsLazilyInstantiated()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            IServiceCollection services = new ServiceCollection();

            int betaInstantiationCount = 0;
            int sigmaInstantiationCount = 0;
            int omegaInstantiationCount = 0;

            services.AddKeyedSingleton<IAlgorithm>("AlgorithmBeta", (sp, _) =>
            {
                betaInstantiationCount++;
                return new AlgorithmBeta();
            });
            services.AddKeyedSingleton<IAlgorithm>("Sigma", (sp, _) =>
            {
                sigmaInstantiationCount++;
                return new AlgorithmSigma();
            });
            services.AddKeyedSingleton<IAlgorithm>("Omega", (sp, _) =>
            {
                omegaInstantiationCount++;
                return new AlgorithmOmega("OMEGA");
            });

            services.AddSingleton(configuration)
                .AddFeatureManagement()
                .AddFeatureFilter<TargetingFilter>()
                .WithVariantService<IAlgorithm>(Features.VariantImplementationFeature);

            var targetingContextAccessor = new OnDemandTargetingContextAccessor();

            services.AddSingleton<ITargetingContextAccessor>(targetingContextAccessor);

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IVariantServiceProvider<IAlgorithm> featuredAlgorithm = serviceProvider.GetRequiredService<IVariantServiceProvider<IAlgorithm>>();

            //
            // No variant resolved yet - nothing should be instantiated.
            Assert.Equal(0, betaInstantiationCount);
            Assert.Equal(0, sigmaInstantiationCount);
            Assert.Equal(0, omegaInstantiationCount);

            //
            // Resolve the Beta variant. Only AlgorithmBeta should be instantiated.
            targetingContextAccessor.Current = new TargetingContext { UserId = "UserBeta" };
            IAlgorithm algorithm = await featuredAlgorithm.GetServiceAsync(CancellationToken.None);
            Assert.Equal("Beta", algorithm.Style);
            Assert.Equal(1, betaInstantiationCount);
            Assert.Equal(0, sigmaInstantiationCount);
            Assert.Equal(0, omegaInstantiationCount);

            //
            // Resolving Beta again should reuse the cached instance - no new instantiation.
            algorithm = await featuredAlgorithm.GetServiceAsync(CancellationToken.None);
            Assert.Equal("Beta", algorithm.Style);
            Assert.Equal(1, betaInstantiationCount);
            Assert.Equal(0, sigmaInstantiationCount);
            Assert.Equal(0, omegaInstantiationCount);

            //
            // Resolve the Sigma variant. Only AlgorithmSigma should be instantiated additionally.
            targetingContextAccessor.Current = new TargetingContext { UserId = "UserSigma" };
            algorithm = await featuredAlgorithm.GetServiceAsync(CancellationToken.None);
            Assert.Equal("Sigma", algorithm.Style);
            Assert.Equal(1, betaInstantiationCount);
            Assert.Equal(1, sigmaInstantiationCount);
            Assert.Equal(0, omegaInstantiationCount);
        }

        [Fact]
        public async Task VariantServiceProviderPrefersKeyedOverNonKeyed()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            IServiceCollection services = new ServiceCollection();

            //
            // Register both keyed and non-keyed implementations matching the same variant name.
            // The keyed registration should take precedence.
            services.AddSingleton<IAlgorithm, AlgorithmBeta>();
            services.AddKeyedSingleton<IAlgorithm>("AlgorithmBeta", (sp, _) => new AlgorithmOmega("KeyedBeta"));

            services.AddSingleton(configuration)
                .AddFeatureManagement()
                .AddFeatureFilter<TargetingFilter>()
                .WithVariantService<IAlgorithm>(Features.VariantImplementationFeature);

            var targetingContextAccessor = new OnDemandTargetingContextAccessor();

            services.AddSingleton<ITargetingContextAccessor>(targetingContextAccessor);

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IVariantServiceProvider<IAlgorithm> featuredAlgorithm = serviceProvider.GetRequiredService<IVariantServiceProvider<IAlgorithm>>();

            targetingContextAccessor.Current = new TargetingContext { UserId = "UserBeta" };
            IAlgorithm algorithm = await featuredAlgorithm.GetServiceAsync(CancellationToken.None);
            Assert.NotNull(algorithm);
            Assert.Equal("KeyedBeta", algorithm.Style);
        }

        [Fact]
        public async Task ContextualVariantServiceProviderUsesProvidedContext()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            IServiceCollection services = new ServiceCollection();

            services.AddSingleton<IAlgorithm, BigAlgorithm>();
            services.AddSingleton<IAlgorithm, SmallAlgorithm>();

            services.AddSingleton(configuration)
                .AddFeatureManagement()
                .AddFeatureFilter<ContextualTestFilter>()
                .WithVariantService<IAlgorithm>(Features.ContextualFeatureWithVariant);

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            ContextualTestFilter contextualTestFeatureFilter = (ContextualTestFilter)serviceProvider
                .GetRequiredService<IEnumerable<IFeatureFilterMetadata>>()
                .First(f => f is ContextualTestFilter);

            contextualTestFeatureFilter.ContextualCallback = (ctx, accountContext) =>
            {
                var allowedAccounts = new List<string>();

                ctx.Parameters.Bind("AllowedAccounts", allowedAccounts);

                return allowedAccounts.Contains(accountContext.AccountId);
            };

            IContextualVariantServiceProvider<IAlgorithm> contextualAlgorithm =
                serviceProvider.GetRequiredService<IContextualVariantServiceProvider<IAlgorithm>>();

            //
            // The provided context enables the feature, so the "default when enabled" variant (Big) is resolved.
            IAlgorithm algorithm = await contextualAlgorithm.GetServiceAsync(
                new AppContext { AccountId = "abc" },
                CancellationToken.None);

            Assert.NotNull(algorithm);
            Assert.Equal("Big", algorithm.Style);

            //
            // The provided context disables the feature, so the "default when disabled" variant (Small) is resolved.
            algorithm = await contextualAlgorithm.GetServiceAsync(
                new AppContext { AccountId = "NotEnabledAccount" },
                CancellationToken.None);

            Assert.NotNull(algorithm);
            Assert.Equal("Small", algorithm.Style);
        }

        [Fact]
        public async Task VariantServiceProviderFallsBackToFeatureStatus()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            //
            // An enabled feature flag with no variants resolves the TEnabled implementation.
            IServiceCollection enabledServices = new ServiceCollection();

            enabledServices.AddSingleton<IAlgorithm, BigAlgorithm>();
            enabledServices.AddSingleton<IAlgorithm, SmallAlgorithm>();

            enabledServices.AddSingleton(configuration)
                .AddFeatureManagement()
                .WithVariantService<IAlgorithm, BigAlgorithm, SmallAlgorithm>(Features.OnTestFeature);

            ServiceProvider enabledServiceProvider = enabledServices.BuildServiceProvider();

            IAlgorithm algorithm = await enabledServiceProvider
                .GetRequiredService<IVariantServiceProvider<IAlgorithm>>()
                .GetServiceAsync(CancellationToken.None);

            Assert.NotNull(algorithm);
            Assert.Equal("Big", algorithm.Style);

            //
            // A disabled feature flag with no variants resolves the TDisabled implementation.
            IServiceCollection disabledServices = new ServiceCollection();

            disabledServices.AddSingleton<IAlgorithm, BigAlgorithm>();
            disabledServices.AddSingleton<IAlgorithm, SmallAlgorithm>();

            disabledServices.AddSingleton(configuration)
                .AddFeatureManagement()
                .WithVariantService<IAlgorithm, BigAlgorithm, SmallAlgorithm>(Features.OffTestFeature);

            ServiceProvider disabledServiceProvider = disabledServices.BuildServiceProvider();

            algorithm = await disabledServiceProvider
                .GetRequiredService<IVariantServiceProvider<IAlgorithm>>()
                .GetServiceAsync(CancellationToken.None);

            Assert.NotNull(algorithm);
            Assert.Equal("Small", algorithm.Style);

            //
            // A feature flag whose assigned variant has no matching service also falls back to the feature status.
            // ContextualFeatureWithVariant is disabled without a context, so the "Small" variant is assigned but no
            // registered service matches it, so the disabled status implementation is resolved instead of null.
            IServiceCollection unmatchedVariantServices = new ServiceCollection();

            unmatchedVariantServices.AddSingleton<IAlgorithm, AlgorithmBeta>();
            unmatchedVariantServices.AddSingleton<IAlgorithm, AlgorithmSigma>();

            unmatchedVariantServices.AddSingleton(configuration)
                .AddFeatureManagement()
                .AddFeatureFilter<ContextualTestFilter>()
                .WithVariantService<IAlgorithm, AlgorithmBeta, AlgorithmSigma>(Features.ContextualFeatureWithVariant);

            ServiceProvider unmatchedVariantServiceProvider = unmatchedVariantServices.BuildServiceProvider();

            algorithm = await unmatchedVariantServiceProvider
                .GetRequiredService<IVariantServiceProvider<IAlgorithm>>()
                .GetServiceAsync(CancellationToken.None);

            Assert.NotNull(algorithm);
            Assert.Equal("Sigma", algorithm.Style);
        }
    }
}
