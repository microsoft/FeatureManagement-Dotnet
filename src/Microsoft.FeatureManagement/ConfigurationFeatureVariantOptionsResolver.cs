// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A feature variant options resolver that resolves options by reading configuration from the .NET Core <see cref="IConfiguration"/> system.
    /// </summary>
    sealed class ConfigurationFeatureVariantOptionsResolver : IFeatureVariantOptionsResolver
    {
        private readonly IConfiguration _configuration;

        public ConfigurationFeatureVariantOptionsResolver(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public ValueTask<T> GetOptions<T>(FeatureDefinition featureDefinition, FeatureVariant variant, CancellationToken cancellationToken)
        {
            if (variant == null)
            {
                throw new ArgumentNullException(nameof(variant));
            }

            IConfiguration configuration = _configuration.GetSection($"{variant.ConfigurationReference}");

            return new ValueTask<T>(configuration.Get<T>());
        }
    }
}
