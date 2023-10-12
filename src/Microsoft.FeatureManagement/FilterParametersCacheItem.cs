// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Parameters of <see cref="IFeatureFilter"/>s which implement <see cref="IFilterParametersBinder"/> can be cached in <see cref="IFilterParametersCache"/>
    /// </summary>
    public class FilterParametersCacheItem
    {
        /// <summary>
        /// Feature filter parameters from <see cref="FeatureFilterConfiguration"/>
        /// </summary>
        public IConfiguration Parameters { get; set; }

        /// <summary>
        /// Settings bound from feature filter parameters.
        /// </summary>
        public object Settings { get; set; }
    }
}
