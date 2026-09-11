// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Extensions for <see cref="Variant"/>.
    /// </summary>
    public static class VariantExtensions
    {
        /// <summary>
        /// Gets the variant configuration as the requested type.
        /// </summary>
        /// <typeparam name="T">The type of the configuration.</typeparam>
        /// <param name="variant">The variant to read.</param>
        /// <returns>
        /// The supplied configuration object when <see cref="Variant.ConfigurationObject"/> assignable to <typeparamref name="T"/>;
        /// otherwise, the configuration bound to <typeparamref name="T"/> from <see cref="Variant.Configuration"/>.
        /// Returns <c>default</c> when the variant or its configuration is absent.
        /// </returns>
        public static T GetConfiguration<T>(this Variant variant)
        {
            if (variant == null)
            {
                return default;
            }

            if (variant.ConfigurationObject is T typedConfigurationObject)
            {
                return typedConfigurationObject;
            }

            return variant.Configuration != null
                ? variant.Configuration.Get<T>()
                : default;
        }
    }
}
