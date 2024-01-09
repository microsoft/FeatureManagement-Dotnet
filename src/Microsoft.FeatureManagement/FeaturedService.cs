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
        private IEnumerable<FeaturedServiceImplementationWrapper<TService>> _services;

        private IVariantFeatureManager _featureManager;

        private string _featureName { get; init; }

        public FeaturedService(string featureName, IEnumerable<FeaturedServiceImplementationWrapper<TService>> services, IVariantFeatureManager featureManager)
        {
            _featureName = featureName;

            _services = services.Where(s => s.FeatureName.Equals(featureName));

            _featureManager = featureManager;
        }

        public async ValueTask<TService> GetAsync(CancellationToken cancellationToken)
        {
            Variant variant = await _featureManager.GetVariantAsync(_featureName, cancellationToken);

            if (variant != null)
            {
                foreach (var item in _services) 
                {
                    if(item.VariantName == variant.Name)
                    {
                        return item.Implementation;
                    }
                }
            }

            return default;
        }
    }
}
