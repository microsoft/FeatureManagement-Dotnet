// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Contextual information needed during the process of feature variant assignment
    /// </summary>
    public class FeatureVariantAssignmentContext
    {
        /// <summary>
        /// The definition of the dynamic feature in need of an assigned variant
        /// </summary>
        public DynamicFeatureDefinition FeatureDefinition { get; set; }

        /// <summary>
        /// A map of variants and associated assignment settings, if any, that have been pre-bound from <see cref="FeatureVariant.AssignmentParameters"/>.
        /// The settings are made available for <see cref="IFeatureVariantAssigner"/>'s that implement <see cref="IFilterParametersBinder"/>.
        /// </summary>
        public IDictionary<FeatureVariant, object> AssignmentSettings { get; set; }
    }
}
