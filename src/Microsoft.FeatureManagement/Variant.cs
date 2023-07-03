// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;

namespace Microsoft.FeatureManagement
{
    public class Variant
    {
        public string Name { get; set; }

        public IConfiguration Configuration { get; set; }
    }
}
