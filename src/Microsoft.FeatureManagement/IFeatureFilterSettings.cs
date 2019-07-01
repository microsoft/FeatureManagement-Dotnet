// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// The settings that define a feature filter.
    /// </summary>
    interface IFeatureFilterSettings
    {
        /// <summary>
        /// The name of the feature filer.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Configurable parameters that can change across instances of a feature filter.
        /// </summary>
        IConfiguration Parameters { get; }
    }
}
