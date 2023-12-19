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
        TService _defaultImplementation;

        IEnumerable<FeaturedServiceImplementationWrapper<TService>> _services;

        FeaturedServiceImplementationWrapper<TService> _featureBasedImplementation;

        IEnumerable<FeaturedServiceImplementationWrapper<TService>> _variantBasedImplementation;

        IVariantFeatureManager _featureManager;

        string _feature;

        public FeaturedService(TService defaultImplementation, IEnumerable<FeaturedServiceImplementationWrapper<TService>> services, IVariantFeatureManager featureManager)
        {
            if (!services.All(s => s.FeatureName == services.First().FeatureName)) 
            {
                throw new ArgumentException();
            }

            _defaultImplementation = defaultImplementation;

            _feature = services.First()?.FeatureName;

            _services = services;

            _featureBasedImplementation = _services.FirstOrDefault(s => s.ForVariant == false);

            _variantBasedImplementation = _services.Where(s => s.ForVariant);

            _featureManager = featureManager;
        }

        public async Task<TService> GetAsync(CancellationToken cancellationToken)
        {
            bool isEnabled = await _featureManager.IsEnabledAsync(_feature, cancellationToken);

            Variant variant = await _featureManager.GetVariantAsync(_feature, cancellationToken);

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
