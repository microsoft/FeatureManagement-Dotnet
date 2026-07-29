// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement.FeatureFilters;
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
    internal class VariantServiceProvider<TService> : IContextualVariantServiceProvider<TService> where TService : class
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IVariantFeatureManager _featureManager;
        private readonly string _featureName;
        private readonly Type _fallbackWhenEnabled;
        private readonly Type _fallbackWhenDisabled;
        private readonly ConcurrentDictionary<string, TService> _variantServiceCache;

        /// <summary>
        /// Creates a variant service provider.
        /// </summary>
        /// <param name="featureName">The feature flag that should be used to determine which variant of the service should be used.</param>
        /// <param name="featureManager">The feature manager to get the assigned variant of the feature flag.</param>
        /// <param name="serviceProvider">The service provider used to resolve implementation variants of TService. If it implements <see cref="IKeyedServiceProvider"/>, keyed resolution is used to enable lazy instantiation; otherwise all registered implementations are enumerated.</param>
        /// <param name="fallbackWhenEnabled">The implementation type to fall back to when the feature flag has no assigned variant and is enabled. If null, no status-based fallback is performed.</param>
        /// <param name="fallbackWhenDisabled">The implementation type to fall back to when the feature flag has no assigned variant and is disabled. If null, no status-based fallback is performed.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureName"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureManager"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceProvider"/> is null.</exception>
        public VariantServiceProvider(string featureName, IVariantFeatureManager featureManager, IServiceProvider serviceProvider, Type fallbackWhenEnabled = null, Type fallbackWhenDisabled = null)
        {
            _featureName = featureName ?? throw new ArgumentNullException(nameof(featureName));
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _fallbackWhenEnabled = fallbackWhenEnabled;
            _fallbackWhenDisabled = fallbackWhenDisabled;
            _variantServiceCache = new ConcurrentDictionary<string, TService>();
        }

        /// <summary>
        /// Gets implementation of TService according to the assigned variant from the feature flag.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An implementation matched with the assigned variant. If there is no matched implementation, it will return null.</returns>
        public ValueTask<TService> GetServiceAsync(CancellationToken cancellationToken)
        {
            //
            // Explicitly evaluate without a context so contextual feature filters are not invoked with a null context.
            return GetServiceAsync<object>(context: null, useContext: false, cancellationToken);
        }

        /// <summary>
        /// Gets implementation of TService according to the assigned variant from the feature flag, using the provided context to evaluate contextual feature filters.
        /// </summary>
        /// <typeparam name="TContext">The type of the context.</typeparam>
        /// <param name="context">A context that provides information used to evaluate contextual feature filters and to determine the assigned variant.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An implementation matched with the assigned variant. If there is no matched implementation, it will return null.</returns>
        public ValueTask<TService> GetServiceAsync<TContext>(TContext context, CancellationToken cancellationToken)
        {
            return GetServiceAsync(context, useContext: true, cancellationToken);
        }

        private async ValueTask<TService> GetServiceAsync<TContext>(TContext context, bool useContext, CancellationToken cancellationToken)
        {
            Debug.Assert(_featureName != null);

            Variant variant = null;

            //
            // Only a targeting context (or contextless evaluation) can assign a variant. A non-targeting context is
            // deliberately supplied for contextual filters, so it is used only for the feature status fallback below.
            if (useContext && context is ITargetingContext targetingContext)
            {
                variant = await _featureManager.GetVariantAsync(_featureName, targetingContext, cancellationToken);
            }
            else if (!useContext)
            {
                variant = await _featureManager.GetVariantAsync(_featureName, cancellationToken);
            }

            if (variant != null)
            {
                TService implementation = _variantServiceCache.GetOrAdd(variant.Name, ResolveVariantService);

                if (implementation != null)
                {
                    return implementation;
                }
            }

            //
            // No implementation resolved from a variant. Fall back to the feature status.
            bool enabled;

            if (useContext)
            {
                enabled = await _featureManager.IsEnabledAsync(_featureName, context, cancellationToken);
            }
            else
            {
                enabled = await _featureManager.IsEnabledAsync(_featureName, cancellationToken);
            }

            Type implementationType = enabled ? _fallbackWhenEnabled : _fallbackWhenDisabled;

            if (implementationType != null)
            {
                return _variantServiceCache.GetOrAdd(GetVariantServiceName(implementationType), ResolveVariantService);
            }

            return null;
        }

        private TService ResolveVariantService(string variantName)
        {
            //
            // If the service provider supports keyed services, try to resolve the variant by its name as the key first.
            // This allows lazy instantiation of the variant service.
            if (_serviceProvider is IKeyedServiceProvider)
            {
                TService keyedService = _serviceProvider.GetKeyedService<TService>(variantName);

                if (keyedService != null)
                {
                    return keyedService;
                }
            }

            //
            // Fall back to enumerating all non-keyed registrations of TService and matching by VariantServiceAliasAttribute or the implementation type name.
            IEnumerable<TService> services = _serviceProvider.GetRequiredService<IEnumerable<TService>>();

            return services.FirstOrDefault(
                service => IsMatchingVariantName(service.GetType(), variantName));
        }

        private bool IsMatchingVariantName(Type implementationType, string variantName)
        {
            return string.Equals(GetVariantServiceName(implementationType), variantName, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetVariantServiceName(Type implementationType)
        {
            return ((VariantServiceAliasAttribute)Attribute.GetCustomAttribute(implementationType, typeof(VariantServiceAliasAttribute)))?.Alias
                ?? implementationType.Name;
        }
    }
}
