// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    internal class FeaturedService<TService> : IFeaturedService<TService> where TService : class
    {
        private string _featureName;
        private IVariantFeatureManager _featureManager;
        private IEnumerable<FeaturedServiceImplementationWrapper<TService>> _services;

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
                FeaturedServiceImplementationWrapper<TService> implementationWrapper = _services.FirstOrDefault(s => 
                    IsMatchingVariant(
                        s.Implementation.GetType(),
                        variant));

                if (implementationWrapper != null)
                {
                    return implementationWrapper.Implementation;
                }
            }

            return null;
        }

        private bool IsMatchingVariant(Type implementationType, Variant variant)
        {
            Debug.Assert(variant != null);

            string implementationName = ((FeaturedServiceAliasAttribute)Attribute.GetCustomAttribute(implementationType, typeof(FeaturedServiceAliasAttribute)))?.Alias;

            if (implementationName == null)
            {
                implementationName = implementationType.Name;
            }

            string variantConfiguration = variant.Configuration?.Value;

            if (variantConfiguration != null && string.Equals(implementationName, variantConfiguration, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(implementationName, variant.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
