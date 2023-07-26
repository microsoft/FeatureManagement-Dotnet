// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Allows the name of an <see cref="IFeatureVariantAllocator"/> to be customized to relate to the name specified in configuration.
    /// </summary>
    public class AllocatorAliasAttribute : Attribute
    {
        /// <summary>
        /// Creates an allocator alias using the provided alias.
        /// </summary>
        /// <param name="alias">The alias of the feature variant allocator.</param>
        public AllocatorAliasAttribute(string alias)
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw new ArgumentNullException(nameof(alias));
            }

            Alias = alias;
        }

        /// <summary>
        /// The name that will be used to match feature feature variant allocator specified in the configuration.
        /// </summary>
        public string Alias { get; }
    }
}
