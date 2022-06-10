// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// The definition of a dynamic feature.
    /// </summary>
    public class DynamicFeatureDefinition
    {
        /// <summary>
        /// The name of the dynamic feature.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The assigner used to pick the variant that should be used when a variant is requested
        /// </summary>
        public string Assigner { get; set; }

        /// <summary>
        /// The feature variants listed for this dynamic feature.
        /// </summary>
        public IEnumerable<FeatureVariant> Variants { get; set; } = Enumerable.Empty<FeatureVariant>();
    }
}
