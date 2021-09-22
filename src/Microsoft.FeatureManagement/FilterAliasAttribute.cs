// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Allows the name of an <see cref="IFeatureFilter"/> to be customized to relate to the name specified in configuration.
    /// </summary>
    public class FilterAliasAttribute : Attribute
    {
        /// <summary>
        /// Creates a filter alias using the provided alias.
        /// </summary>
        /// <param name="alias">The alias of the feature filter.</param>
        public FilterAliasAttribute(string alias)
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw new ArgumentNullException(nameof(alias));
            }

            Alias = alias;
        }

        /// <summary>
        /// The name that will be used to match feature filters specified in the configuration.
        /// </summary>
        public string Alias { get; }
    }
}
