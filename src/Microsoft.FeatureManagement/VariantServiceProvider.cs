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
        private readonly ConcurrentDictionary<string, TService> _variantServiceCache;

        /// <summary>
        /// Creates a variant service provider.
        /// </summary>
        /// <param name="featureName">The feature flag that should be used to determine which variant of the service should be used.</param>
        /// <param name="featureManager">The feature manager to get the assigned variant of the feature flag.</param>
        /// <param name="serviceProvider">The service provider used to resolve implementations of TService.</param>
        /// <param name="options">Options used to configure the variant service provider.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureName"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureManager"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceProvider"/> is null.</exception>
        public VariantServiceProvider(string featureName, IVariantFeatureManager featureManager, IServiceProvider serviceProvider, VariantServiceProviderOptions options = null)
        {
            _featureName = featureName ?? throw new ArgumentNullException(nameof(featureName));
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options;
            _variantServiceCache = new ConcurrentDictionary<string, TService>();
        }

        /// <summary>
        /// Gets implementation of TService according to the assigned variant from the feature flag.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An implementation matched with the assigned variant. If there is no matched implementation, it will return null.</returns>
        public async ValueTask<TService> GetServiceAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_featureName != null);

            TService implementation = await ResolveByVariantAsync(cancellationToken);

            if (implementation != null)
            {
                return implementation;
            }

            return await ResolveByStatusAsync(cancellationToken);
        }

        private async ValueTask<TService> ResolveByVariantAsync(CancellationToken cancellationToken)
        {
            Variant variant = await _featureManager.GetVariantAsync(_featureName, cancellationToken);

            if (variant == null)
            {
                return null;
            }

            return _variantServiceCache.GetOrAdd(
                variant.Name,
                (name) => ResolveVariantService(name));
        }

        private async ValueTask<TService> ResolveByStatusAsync(CancellationToken cancellationToken)
        {
            if (_options == null)
            {
                return null;
            }

            bool isEnabled = await _featureManager.IsEnabledAsync(_featureName, cancellationToken);

            string alias = isEnabled ? _options.FallbackWhenEnabled : _options.FallbackWhenDisabled;

            if (string.IsNullOrEmpty(alias))
            {
                return null;
            }

            return _variantServiceCache.GetOrAdd(
                alias,
                (name) => ResolveVariantService(name));
        }

        private TService ResolveVariantService(string name)
        {
            //
            // Prefer keyed resolution when supported. This enables lazy instantiation of variant implementations.
            if (_serviceProvider is IKeyedServiceProvider keyedServiceProvider)
            {
                TService keyedVariantService = keyedServiceProvider.GetKeyedService<TService>(name);

                if (keyedVariantService != null)
                {
                    return keyedVariantService;
                }
            }

            //
            // Fall back to scanning non-keyed registrations and matching by VariantServiceAliasAttribute or type name.
            return _serviceProvider
                .GetRequiredService<IEnumerable<TService>>()
                .FirstOrDefault(service => IsMatchingVariantName(service.GetType(), name));
        }

        private static bool IsMatchingVariantName(Type implementationType, string name)
        {
            string implementationName = ((VariantServiceAliasAttribute)Attribute.GetCustomAttribute(implementationType, typeof(VariantServiceAliasAttribute)))?.Alias;

            if (implementationName == null)
            {
                implementationName = implementationType.Name;
            }

            return string.Equals(implementationName, name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
