// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A variant for a feature.
    /// </summary>
    public class Variant
    {
        /// <summary>
        /// The name of the variant.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The configuration of the variant.
        /// </summary>
        public IConfigurationSection Configuration { get; set; }

        /// <summary>
        /// The configuration of the variant.
        /// When set, variants should prefer this over <see cref="Configuration"/>.
        /// </summary>
        public object ConfigurationObject { get; set; }
    }
}
