// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// The settings that define a feature filter.
    /// </summary>
    class FeatureFilterSettings
    {
        /// <summary>
        /// The name of the feature filer.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Configurable parameters that can change across instances of a feature filter.
        /// </summary>
        public IConfiguration Parameters { get; set; } = new ConfigurationRoot(new List<IConfigurationProvider>());
    }
}
