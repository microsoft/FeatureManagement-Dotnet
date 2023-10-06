// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;

namespace Microsoft.FeatureManagement.Telemetry
{
    /// <summary>
    /// A container for metadata relevant to telemetry.
    /// </summary>
    public class TelemetryMetadata
    {
        /// <summary>
        /// Metadata that can be used to group feature flags.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// An ETag that is used to track when the feature definiton has changed.
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// A dictionary of tags used to assign additional metadata to a feature.
        /// </summary>
        public IReadOnlyDictionary<string, string> Tags { get; set; }
    }
}
