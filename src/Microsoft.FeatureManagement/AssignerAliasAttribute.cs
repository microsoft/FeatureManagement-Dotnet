// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Allows the name of an <see cref="IFeatureVariantAssigner"/> to be customized to relate to the name specified in configuration.
    /// </summary>
    public class AssignerAliasAttribute : Attribute
    {
        /// <summary>
        /// Creates an assigner alias using the provided alias.
        /// </summary>
        /// <param name="alias">The alias of the feature variant assigner.</param>
        public AssignerAliasAttribute(string alias)
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw new ArgumentNullException(alias);
            }

            Alias = alias;
        }

        /// <summary>
        /// The name that will be used to match feature feature variant assigners specified in the configuration.
        /// </summary>
        public string Alias { get; }
    }
}
