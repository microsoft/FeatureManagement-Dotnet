// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.Extensions.Configuration;

namespace Microsoft.FeatureManagement
{
    internal class VariantConfigurationSection : ConfigurationSection
    {
        public VariantConfigurationSection(ConfigurationRoot root, string path) : base(root, path) {}


    }
}
