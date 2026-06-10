using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <inheritdoc/>
    internal class FeatureServiceProvider<TService, TEnabled, TDisabled> : IFeatureServiceProvider<TService>
        where TService : class
        where TEnabled : class, TService
        where TDisabled : class, TService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IFeatureManager _featureManager;
        private readonly string _featureName;
        private readonly FeatureServiceProviderOptions _options;
        private TService _enabledService;
        private TService _disabledService;

        /// <summary>
        /// Creates a feature service provider.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve implementation variants of TService. If it implements <see cref="IKeyedServiceProvider"/>, keyed resolution is used to enable lazy instantiation; otherwise all registered implementations are enumerated.</param>
        /// <param name="featureManager">The feature manager to get the assigned variant of the feature flag.</param>
        /// <param name="featureName">The feature flag that should be used to determine which variant of the service should be used.</param>
        /// <param name="options">Options used to configure the feature service provider.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceProvider"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureManager"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="featureName"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        public FeatureServiceProvider(IServiceProvider serviceProvider, IFeatureManager featureManager, string featureName, FeatureServiceProviderOptions options)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
            _featureName = featureName ?? throw new ArgumentNullException(nameof(featureName));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc/>
        public ValueTask<TService> GetServiceAsync()
        {
            return GetServiceAsync<object>(null);
        }

        /// <inheritdoc/>
        public async ValueTask<TService> GetServiceAsync<TContext>(TContext context)
        {
            var isEnabled = await _featureManager.IsEnabledAsync(_featureName, context);

            var implementation = isEnabled ? _enabledService : _disabledService;
            if (implementation != null)
            {
                return implementation;
            }

            if (_serviceProvider is IKeyedServiceProvider keyedServiceProvider)
            {
                implementation = isEnabled
                    ? _enabledService ??= keyedServiceProvider.GetKeyedService<TService>(_options.EnabledKey)
                    : _disabledService ??= keyedServiceProvider.GetKeyedService<TService>(_options.DisabledKey);
                if (implementation != null)
                {
                    return implementation;
                }
            }

            return isEnabled
                ? _enabledService ??= _serviceProvider.GetServices<TService>().OfType<TEnabled>().FirstOrDefault()
                : _disabledService ??= _serviceProvider.GetServices<TService>().OfType<TDisabled>().FirstOrDefault();
        }
    }
}
