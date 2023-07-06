// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// 
    /// </summary>
    public class Variant
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IConfiguration Configuration { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ConfigurationValue { get; set; }
    }
}
