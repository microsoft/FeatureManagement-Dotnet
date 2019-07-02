// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Implementation of <see cref="IFeatureFilterSettings"/>.
    /// </summary>
    class FeatureFilterSettings : IFeatureFilterSettings
    {
        public string Name { get; set; }

        public IConfiguration Parameters { get; set; } = new ConfigurationRoot(new List<IConfigurationProvider>());
    }
}
