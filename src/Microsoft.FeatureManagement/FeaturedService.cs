// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    internal class FeaturedService<TService> : IFeaturedService<TService>
    {
        private TService _defaultImplementation;

        private IEnumerable<FeaturedServiceImplementationWrapper<TService>> _services;

        private FeaturedServiceImplementationWrapper<TService> _featureBasedImplementation;

        private IEnumerable<FeaturedServiceImplementationWrapper<TService>> _variantBasedImplementation;

        private IVariantFeatureManager _featureManager;

        private string _featureName;

        public FeaturedService(TService defaultImplementation, IEnumerable<FeaturedServiceImplementationWrapper<TService>> services, IVariantFeatureManager featureManager)
        {
            if (!services.All(s => s.FeatureName == services.First().FeatureName)) 
            {
                throw new ArgumentException();
            }

            _defaultImplementation = defaultImplementation;

            _featureName = services.First()?.FeatureName;

            _services = services;

            _featureBasedImplementation = _services.FirstOrDefault(s => s.VariantBased == false);

            _variantBasedImplementation = _services.Where(s => s.VariantBased);

            _featureManager = featureManager;
        }

        public async ValueTask<TService> GetAsync(CancellationToken cancellationToken)
        {
            bool isEnabled = await _featureManager.IsEnabledAsync(_featureName, cancellationToken);

            Variant variant = await _featureManager.GetVariantAsync(_featureName, cancellationToken);

            if (variant != null)
            {
                foreach (var item in _variantBasedImplementation) 
                {
                    if(item.VariantName == variant.Name)
                    {
                        return item.Implementation;
                    }
                }
            }

            if (isEnabled)
            {
                return _featureBasedImplementation.Implementation;
            }

            return _defaultImplementation;
        }
    }
}
