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
    /// Used to get different implementations of TService depending on either the assigned variant or the enabled status of a feature flag.
    /// </summary>
    internal class VariantServiceProvider<TService> : IVariantServiceProvider<TService> where TService : class
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IVariantFeatureManager _featureManager;
        private readonly string _featureName;
        private readonly VariantServiceMatchMode _matchMode;
        private readonly ConcurrentDictionary<string, TService> _variantServiceCache;

        /// <summary>
        /// Creates a variant service provider that selects an implementation based on the assigned variant of the feature flag.
        /// </summary>
        /// <param name="featureName">The feature flag that should be used to determine which variant of the service should be used.</param>
        /// <param name="featureManager">The feature manager to get the assigned variant of the feature flag.</param>
        /// <param name="serviceProvider">The service provider used to resolve implementation variants of TService. If it implements <see cref="IKeyedServiceProvider"/>, keyed resolution is used to enable lazy instantiation; otherwise all registered implementations are enumerated.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureName"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureManager"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceProvider"/> is null.</exception>
        public VariantServiceProvider(string featureName, IVariantFeatureManager featureManager, IServiceProvider serviceProvider)
            : this(featureName, featureManager, serviceProvider, VariantServiceMatchMode.Variant)
        {
        }

        /// <summary>
        /// Creates a variant service provider that selects an implementation based on the assigned variant or the enabled status of the feature flag.
        /// </summary>
        /// <param name="featureName">The feature flag that should be used to determine which variant of the service should be used.</param>
        /// <param name="featureManager">The feature manager to evaluate the feature flag.</param>
        /// <param name="serviceProvider">The service provider used to resolve implementation variants of TService.</param>
        /// <param name="matchMode">Describes whether the implementation is matched by variant name or by feature flag status.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureName"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureManager"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceProvider"/> is null.</exception>
        public VariantServiceProvider(string featureName, IVariantFeatureManager featureManager, IServiceProvider serviceProvider, VariantServiceMatchMode matchMode)
        {
            _featureName = featureName ?? throw new ArgumentNullException(nameof(featureName));
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _matchMode = matchMode;
            _variantServiceCache = new ConcurrentDictionary<string, TService>();
        }

        /// <summary>
        /// Gets the implementation of TService matched against the assigned variant or the enabled status of the feature flag.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An implementation matched with the assigned variant or status. If there is no matched implementation, it will return null.</returns>
        public async ValueTask<TService> GetServiceAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_featureName != null);

            if (_matchMode == VariantServiceMatchMode.Status)
            {
                bool isEnabled = await _featureManager.IsEnabledAsync(_featureName, cancellationToken);

                string statusKey = isEnabled ? bool.TrueString : bool.FalseString;

                return _variantServiceCache.GetOrAdd(
                    statusKey,
                    (_) => ResolveByStatus(isEnabled));
            }

            Variant variant = await _featureManager.GetVariantAsync(_featureName, cancellationToken);

            if (variant == null)
            {
                return null;
            }

            return _variantServiceCache.GetOrAdd(
                variant.Name,
                (variantName) => ResolveByVariant(variantName));
        }

        private TService ResolveByVariant(string variantName)
        {
            if (_serviceProvider is IKeyedServiceProvider)
            {
                TService keyedService = _serviceProvider.GetKeyedService<TService>(variantName);

                if (keyedService != null)
                {
                    return keyedService;
                }
            }

            IEnumerable<TService> services = _serviceProvider.GetRequiredService<IEnumerable<TService>>();

            return services.FirstOrDefault(
                service => IsMatchingVariantName(service.GetType(), variantName));
        }

        private TService ResolveByStatus(bool enabled)
        {
            if (_serviceProvider is IKeyedServiceProvider)
            {
                TService keyedService = _serviceProvider.GetKeyedService<TService>(enabled);

                if (keyedService != null)
                {
                    return keyedService;
                }
            }

            IEnumerable<TService> services = _serviceProvider.GetRequiredService<IEnumerable<TService>>();

            return services.FirstOrDefault(
                service => IsMatchingStatus(service.GetType(), enabled));
        }

        private static bool IsMatchingVariantName(Type implementationType, string variantName)
        {
            var attribute = (VariantServiceAliasAttribute)Attribute.GetCustomAttribute(implementationType, typeof(VariantServiceAliasAttribute));

            //
            // Implementations explicitly declared as status-bound do not participate in variant-name matching.
            if (attribute != null && attribute.MatchMode == VariantServiceMatchMode.Status)
            {
                return false;
            }

            string implementationName = attribute?.Alias ?? implementationType.Name;

            return string.Equals(implementationName, variantName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMatchingStatus(Type implementationType, bool enabled)
        {
            var attribute = (VariantServiceAliasAttribute)Attribute.GetCustomAttribute(implementationType, typeof(VariantServiceAliasAttribute));

            //
            // Only implementations explicitly declared as status-bound participate in status matching.
            if (attribute == null || attribute.MatchMode != VariantServiceMatchMode.Status)
            {
                return false;
            }

            string expected = enabled ? bool.TrueString : bool.FalseString;

            return string.Equals(attribute.Alias, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
