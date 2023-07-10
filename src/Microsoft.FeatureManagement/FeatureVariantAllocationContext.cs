// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Contextual information needed during the process of feature variant allocation
    /// </summary>
    public class FeatureVariantAllocationContext
    {
        /// <summary>
        /// The definition of the feature in need of an allocated variant
        /// </summary>
        public FeatureDefinition FeatureDefinition { get; set; }
    }
}