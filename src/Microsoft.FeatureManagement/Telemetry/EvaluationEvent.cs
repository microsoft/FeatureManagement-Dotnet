// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.FeatureFilters;

namespace Microsoft.FeatureManagement.Telemetry
{
    /// <summary>
    /// An event representing the evaluation of a feature.
    /// </summary>
    public class EvaluationEvent
    {
        /// <summary>
        /// The definition of the feature that was evaluated.
        /// </summary>
        public FeatureDefinition FeatureDefinition { get; set; }

        /// <summary>
        /// The enabled state of the feature after evaluation.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// The variant given after evaluation.
        /// </summary>
        public Variant Variant { get; set; }
    }
}
