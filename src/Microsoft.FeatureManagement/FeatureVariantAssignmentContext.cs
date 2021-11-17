// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Contextual information needed during the process of feature variant assignment
    /// </summary>
    public class FeatureVariantAssignmentContext
    {
        /// <summary>
        /// The definition of the feature in need of an assigned variant
        /// </summary>
        public FeatureDefinition FeatureDefinition { get; set; }
    }
}
