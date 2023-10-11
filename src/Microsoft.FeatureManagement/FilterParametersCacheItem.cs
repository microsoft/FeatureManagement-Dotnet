// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;

namespace Microsoft.FeatureManagement
{
    public class FilterParametersCacheItem
    {
        public IConfiguration Parameters { get; set; }

        public object Settings { get; set; }
    }
}
