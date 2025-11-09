// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to get different implementations of TService depending on the assigned variant from a specific variant feature flag.
    /// </summary>
    internal class VariantServiceProvider<TService> : IVariantServiceProvider<TService> where TService : class
    {
        private readonly IVariantFeatureManager _featureManager;
        private readonly string _featureName;
        private readonly ConcurrentDictionary<string, TService> _variantServiceCache;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, ServiceDescriptor> _variantNameToDescriptor; // ImplementationType/Instance descriptors mapped by variant name.
        private readonly List<ServiceDescriptor> _factoryDescriptors; // Descriptors that require factory invocation to discover variant name.

        /// <summary>
        /// Creates a variant service provider.
        /// </summary>
        /// <param name="featureName">The feature flag that should be used to determine which variant of the service should be used.</param>
        /// <param name="featureManager">The feature manager to get the assigned variant of the feature flag.</param>
        /// <param name="serviceDescriptors">Service descriptors for implementation variants of TService.</param>
        /// <param name="serviceProvider">The service provider / scope used to activate implementations lazily.</param>
        public VariantServiceProvider(string featureName, IVariantFeatureManager featureManager, IEnumerable<ServiceDescriptor> serviceDescriptors, IServiceProvider serviceProvider)
        {
            _featureName = featureName ?? throw new ArgumentNullException(nameof(featureName));
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
            if (serviceDescriptors == null) throw new ArgumentNullException(nameof(serviceDescriptors));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _variantServiceCache = new ConcurrentDictionary<string, TService>(StringComparer.OrdinalIgnoreCase);
            _variantNameToDescriptor = new Dictionary<string, ServiceDescriptor>(StringComparer.OrdinalIgnoreCase);
            _factoryDescriptors = new List<ServiceDescriptor>();

            // Precompute mapping for descriptors whose variant name can be determined without instantiation.
            foreach (ServiceDescriptor descriptor in serviceDescriptors)
            {
                if (descriptor.ImplementationType != null)
                {
                    string name = GetVariantName(descriptor.ImplementationType);
                    if (!_variantNameToDescriptor.ContainsKey(name))
                    {
                        _variantNameToDescriptor.Add(name, descriptor);
                    }
                }
                else if (descriptor.ImplementationInstance != null)
                {
                    string name = GetVariantName(descriptor.ImplementationInstance.GetType());
                    if (!_variantNameToDescriptor.ContainsKey(name))
                    {
                        _variantNameToDescriptor.Add(name, descriptor);
                    }
                }
                else if (descriptor.ImplementationFactory != null)
                {
                    // Factory descriptors require instantiation to discover variant name; hold for later.
                    _factoryDescriptors.Add(descriptor);
                }
            }
        }

        /// <summary>
        /// Gets implementation of TService according to the assigned variant from the feature flag.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An implementation matched with the assigned variant. If there is no matched implementation, it will return null.</returns>
        public async ValueTask<TService> GetServiceAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_featureName != null);

            Variant variant = await _featureManager.GetVariantAsync(_featureName, cancellationToken).ConfigureAwait(false);

            if (variant == null)
            {
                return null;
            }

            return _variantServiceCache.GetOrAdd(variant.Name, ResolveVariant);
        }

        private TService ResolveVariant(string variantName)
        {
            // Try fast path using precomputed mapping.
            if (_variantNameToDescriptor.TryGetValue(variantName, out ServiceDescriptor descriptor))
            {
                return ActivateDescriptor(descriptor);
            }

            // Need to probe factory descriptors lazily.
            foreach (ServiceDescriptor factoryDescriptor in _factoryDescriptors)
            {
                TService instance = ActivateDescriptor(factoryDescriptor);

                if (instance == null)
                {
                    continue;
                }

                string discoveredName = GetVariantName(instance.GetType());

                // Cache the mapping for future lookups.
                if (!_variantNameToDescriptor.ContainsKey(discoveredName))
                {
                    _variantNameToDescriptor.Add(discoveredName, factoryDescriptor);
                }

                if (string.Equals(discoveredName, variantName, StringComparison.OrdinalIgnoreCase))
                {
                    return instance;
                }
            }

            return null;
        }

        private TService ActivateDescriptor(ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationInstance != null)
            {
                return (TService)descriptor.ImplementationInstance;
            }

            if (descriptor.ImplementationType != null)
            {
                // Use ActivatorUtilities to honor DI for dependencies of the implementation type.
                return (TService)ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, descriptor.ImplementationType);
            }

            if (descriptor.ImplementationFactory != null)
            {
                return (TService)descriptor.ImplementationFactory(_serviceProvider);
            }

            return null;
        }

        private string GetVariantName(Type implementationType)
        {
            string implementationName = ((VariantServiceAliasAttribute)Attribute.GetCustomAttribute(implementationType, typeof(VariantServiceAliasAttribute)))?.Alias;

            if (implementationName == null)
            {
                implementationName = implementationType.Name;
            }

            return implementationName;
        }
    }
}
