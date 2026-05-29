// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to get different implementations of TService depending on the assigned variant from a specific variant feature flag.
    /// </summary>
    internal class VariantServiceProvider<TService> : IVariantServiceProvider<TService> where TService : class
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IVariantFeatureManager _featureManager;
        private readonly string _featureName;
        private readonly VariantServiceProviderOptions _options;
        private readonly ConcurrentDictionary<object, TService> _variantServiceCache;

        /// <summary>
        /// Creates a variant service provider.
        /// </summary>
        /// <param name="featureName">The feature flag that should be used to determine which variant of the service should be used.</param>
        /// <param name="featureManager">The feature manager to get the assigned variant of the feature flag.</param>
        /// <param name="serviceProvider">The service provider used to resolve implementation variants of TService. If it implements <see cref="IKeyedServiceProvider"/>, keyed resolution is used to enable lazy instantiation; otherwise all registered implementations are enumerated.</param>
        /// <param name="options">Options used to configure the variant service provider.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureName"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureManager"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceProvider"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        public VariantServiceProvider(string featureName, IVariantFeatureManager featureManager, IServiceProvider serviceProvider, VariantServiceProviderOptions options)
        {
            _featureName = featureName ?? throw new ArgumentNullException(nameof(featureName));
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _variantServiceCache = new ConcurrentDictionary<object, TService>();
        }

        /// <summary>
        /// Gets implementation of TService according to the assigned variant from the feature flag.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An implementation matched with the assigned variant. If there is no matched implementation, it will return null.</returns>
        public async ValueTask<TService> GetServiceAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_featureName != null);

            var variantService = await ResolveByVariantAsync(cancellationToken);
            if (variantService != null)
            {
                return variantService;
            }

            return await ResolveByStatusAsync(cancellationToken);
        }

        private async Task<TService> ResolveByVariantAsync(CancellationToken cancellationToken)
        {
            var variant = await _featureManager.GetVariantAsync(_featureName, cancellationToken);

            return variant != null ? _variantServiceCache.GetOrAdd(variant.Name, ResolveVariantService) : null;
        }

        private async Task<TService> ResolveByStatusAsync(CancellationToken cancellationToken)
        {
            var isEnabled = await _featureManager.IsEnabledAsync(_featureName, cancellationToken);
            var statusKey = isEnabled ? _options.FallbackWhenEnabled : _options.FallbackWhenDisabled;

            return statusKey != null ? _variantServiceCache.GetOrAdd(statusKey, ResolveVariantService) : null;
        }

        private TService ResolveVariantService(object variantKey)
        {
            //
            // Prefer keyed resolution when supported. This enables lazy instantiation of variant implementations.
            if (TryGetKeyedVariantService(variantKey, out var keyedVariantService))
            {
                return keyedVariantService;
            }

            if (variantKey is string variantName)
            {
                //
                // Fall back to scanning non-keyed registrations and matching by VariantServiceAliasAttribute or type name.
                return GetVariantServiceFallback(variantName);
            }

            return null;
        }

        private bool TryGetKeyedVariantService(object variantName, out TService keyedService)
        {
            if (_serviceProvider is IKeyedServiceProvider keyedServiceProvider)
            {
                keyedService = keyedServiceProvider.GetKeyedService<TService>(variantName);
                return keyedService != null;
            }

            keyedService = null;
            return false;
        }

        private TService GetVariantServiceFallback(string variantName)
        {
            return _serviceProvider
                .GetRequiredService<IEnumerable<TService>>()
                .FirstOrDefault(service => IsMatchingVariantName(service.GetType(), variantName));
        }

        private static bool IsMatchingVariantName(Type implementationType, string name)
        {
            var implementationName = ((VariantServiceAliasAttribute)Attribute.GetCustomAttribute(implementationType, typeof(VariantServiceAliasAttribute)))?.Alias;

            if (implementationName == null)
            {
                implementationName = implementationType.Name;
            }

            return string.Equals(implementationName, name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
