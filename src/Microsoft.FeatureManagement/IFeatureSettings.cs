// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// The settings for a feature.
    /// </summary>
    interface IFeatureSettings
    {
        /// <summary>
        /// The name of the feature.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The criteria that the feature can be enabled for.
        /// </summary>
        IEnumerable<IFeatureFilterSettings> EnabledFor { get; }
    }
}
