// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Allows the name of a variant service to be customized to relate to the variant name specified in configuration.
    /// </summary>
    public class VariantServiceAliasAttribute : Attribute
    {
        /// <summary>
        /// Creates a variant service alias using the provided alias.
        /// </summary>
        /// <param name="alias">The alias of the variant service.</param>
        public VariantServiceAliasAttribute(string alias)
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw new ArgumentNullException(nameof(alias));
            }

            Alias = alias;
        }

        /// <summary>
        /// Creates a variant service alias whose value matches the default <see cref="VariantServiceProviderOptions.EnabledAlias"/>
        /// or <see cref="VariantServiceProviderOptions.DisabledAlias"/>. This overload is a convenience for toggling between
        /// two service implementations based on the feature flag status with minimal code.
        /// </summary>
        /// <param name="enabled">If true, the alias is set to <see cref="bool.TrueString"/>; otherwise, <see cref="bool.FalseString"/>.</param>
        public VariantServiceAliasAttribute(bool enabled)
        {
            Alias = enabled ? bool.TrueString : bool.FalseString;
        }

        /// <summary>
        /// The name that will be used to match variant name specified in the configuration.
        /// </summary>
        public string Alias { get; }
    }
}
