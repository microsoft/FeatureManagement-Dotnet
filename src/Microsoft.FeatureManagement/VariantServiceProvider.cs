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
    /// Holds factory metadata for lazy service instantiation.
    /// </summary>
    internal class VariantServiceFactory<TService> where TService : class
    {
        private readonly Lazy<Type> _implementationType;

        public Type ImplementationType => _implementationType.Value;
        public Func<TService> Factory { get; }

        public VariantServiceFactory(Type implementationType, Func<TService> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            Factory = factory;

            if (implementationType != null)
            {
                _implementationType = new Lazy<Type>(() => implementationType);
            }
            else
            {
                // For factory-based registrations where type is unknown upfront,
                // we lazily determine it by invoking the factory once.
                // Note: The factory will be invoked to get the type when checking for a match,
                // which means the instance is created at that point. This is acceptable because
                // it only happens when we're looking for a match, and the instance is cached.
                // Alternative approaches would require breaking changes to the registration API.
                _implementationType = new Lazy<Type>(() =>
                {
                    TService instance = factory();
                    return instance?.GetType();
                });
            }
        }
    }

    /// <summary>
    /// Used to get different implementations of TService depending on the assigned variant from a specific variant feature flag.
    /// </summary>
    internal class VariantServiceProvider<TService> : IVariantServiceProvider<TService> where TService : class
    {
        private readonly IEnumerable<VariantServiceFactory<TService>> _serviceFactories;
        private readonly IVariantFeatureManager _featureManager;
        private readonly string _featureName;
        private readonly ConcurrentDictionary<string, TService> _variantServiceCache;

        /// <summary>
        /// Creates a variant service provider.
        /// </summary>
        /// <param name="featureName">The feature flag that should be used to determine which variant of the service should be used.</param>
        /// <param name="featureManager">The feature manager to get the assigned variant of the feature flag.</param>
        /// <param name="serviceFactories">Factory delegates for implementation variants of TService.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureName"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureManager"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceFactories"/> is null.</exception>
        public VariantServiceProvider(string featureName, IVariantFeatureManager featureManager, IEnumerable<VariantServiceFactory<TService>> serviceFactories)
        {
            _featureName = featureName ?? throw new ArgumentNullException(nameof(featureName));
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
            _serviceFactories = serviceFactories ?? throw new ArgumentNullException(nameof(serviceFactories));
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
                    (_) =>
                    {
                        // Find the matching factory by checking the implementation type
                        VariantServiceFactory<TService> matchingFactory = _serviceFactories.FirstOrDefault(
                            factory => IsMatchingVariantName(factory.ImplementationType, variant.Name));

                        // Only invoke the factory if a match is found (lazy instantiation)
                        return matchingFactory?.Factory();
                    }
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
