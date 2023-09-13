// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// The definition of a feature.
    /// </summary>
    public class FeatureDefinition
    {
        /// <summary>
        /// The name of the feature.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The feature filters that the feature can be enabled for.
        /// </summary>
        public IEnumerable<FeatureFilterConfiguration> EnabledFor { get; set; } = new List<FeatureFilterConfiguration>();

        /// <summary>
        /// Determines whether any or all registered feature filters must be enabled for the feature to be considered enabled.
        /// The default value is <see cref="RequirementType.Any"/>.
        /// </summary>
        public RequirementType RequirementType { get; set; } = RequirementType.Any;

        /// <summary>
        /// When set to <see cref="FeatureStatus.Disabled"/>, this feature will always be considered disabled regardless of the rest of the feature definition.
        /// The default value is <see cref="FeatureStatus.Conditional"/>.
        /// </summary>
        public FeatureStatus Status { get; set; } = FeatureStatus.Conditional;

        /// <summary>
        /// Describes how variants should be allocated.
        /// </summary>
        public Allocation Allocation { get; set; }

        /// <summary>
        /// A list of variant definitions that specify a configuration to return when assigned.
        /// </summary>
        public IEnumerable<VariantDefinition> Variants { get; set; } = Enumerable.Empty<VariantDefinition>();
    }
}
