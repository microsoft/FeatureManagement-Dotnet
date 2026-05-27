// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Allows a variant service implementation to be associated with either the assigned variant name
    /// or the enabled status of the bound feature flag.
    /// </summary>
    public class VariantServiceAliasAttribute : Attribute
    {
        /// <summary>
        /// Creates a variant service alias that matches the assigned variant name of the feature flag.
        /// </summary>
        /// <param name="alias">The alias of the variant service. Used to match the assigned variant name specified in the configuration.</param>
        public VariantServiceAliasAttribute(string alias)
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw new ArgumentNullException(nameof(alias));
            }

            Alias = alias;
            MatchMode = VariantServiceMatchMode.Variant;
        }

        /// <summary>
        /// Creates a variant service alias that matches the enabled status of the feature flag.
        /// </summary>
        /// <param name="enabled">Whether the variant service should be selected when the feature flag is enabled (<c>true</c>) or disabled (<c>false</c>).</param>
        public VariantServiceAliasAttribute(bool enabled)
        {
            Alias = enabled ? bool.TrueString : bool.FalseString;
            MatchMode = VariantServiceMatchMode.Status;
        }

        /// <summary>
        /// The name that will be used to match either the assigned variant name or the enabled status of the feature flag.
        /// </summary>
        public string Alias { get; }

        /// <summary>
        /// Describes whether the implementation is matched by variant name or by feature flag status.
        /// </summary>
        public VariantServiceMatchMode MatchMode { get; }
    }
}
