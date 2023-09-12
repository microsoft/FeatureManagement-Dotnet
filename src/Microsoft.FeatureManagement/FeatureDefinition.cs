// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;

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
        /// Determines whether any or all registered feature filters must be enabled for the feature to be considered enabled
        /// The default value is <see cref="RequirementType.Any"/>.
        /// </summary>
        public RequirementType RequirementType { get; set; } = RequirementType.Any;

        /// <summary>
        /// A value used to group configuration settings.
        /// A <see cref="Label"/> is used together with a <see cref="Name"/> to uniquely identify a feature.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// An ETag indicating the state of a feature within a configuration store.
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// A dictionary of tags used to assign additional properties to a feature.
        /// These can be used to indicate how a feature may be applied.
        /// </summary>
        public IReadOnlyDictionary<string, string> Tags { get; set; }

        /// <summary>
        /// A flag to enable or disable sending telemetry events to the registered <see cref="ITelemetryProvider">.
        /// </summary>
        public bool EnableTelemetry { get; set; }
    }
}
