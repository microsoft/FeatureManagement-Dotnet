// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// The settings for a feature.
    /// </summary>
    class FeatureSettings : IFeatureSettings
    {
        /// <summary>
        /// The name of the feature.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The criteria that the feature can be enabled for.
        /// </summary>
        public IEnumerable<IFeatureFilterSettings> EnabledFor { get; set; } = new List<IFeatureFilterSettings>();
    }
}
