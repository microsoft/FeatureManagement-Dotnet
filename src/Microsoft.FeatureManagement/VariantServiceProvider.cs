// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
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
        private readonly IEnumerable<TService> _services;
        private readonly IVariantFeatureManager _featureManager;
        private readonly string _featureName;
        private readonly ConcurrentDictionary<string, TService> _variantServiceCache;

        /// <summary>
        /// Creates a variant service provider.
        /// </summary>
        /// <param name="featureName">The feature flag that should be used to determine which variant of the service should be used.</param>
        /// <param name="featureManager">The feature manager to get the assigned variant of the feature flag.</param>
        /// <param name="services">Implementation variants of TService.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureName"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureManager"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is null.</exception>
        public VariantServiceProvider(string featureName, IVariantFeatureManager featureManager, IEnumerable<TService> services)
        {
            _featureName = featureName ?? throw new ArgumentNullException(nameof(featureName));
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
            _services = services ?? throw new ArgumentNullException(nameof(services));
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

            Variant variant = await _featureManager.GetVariantAsync(_featureName, cancellationToken);

            TService implementation = null;

            if (variant != null)
            {
                implementation = _variantServiceCache.GetOrAdd(
                    variant.Name,
                    (_) => _services.FirstOrDefault(
                        service => IsMatchingVariantName(
                            service.GetType(),
                            variant.Name))
                );
            }

            return implementation;
        }

        private bool IsMatchingVariantName(Type implementationType, string variantName)
        {
            string implementationName = ((VariantServiceAliasAttribute)Attribute.GetCustomAttribute(implementationType, typeof(VariantServiceAliasAttribute)))?.Alias;

            if (implementationName == null)
            {
                implementationName = implementationType.Name;
            }

            return string.Equals(implementationName, variantName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
