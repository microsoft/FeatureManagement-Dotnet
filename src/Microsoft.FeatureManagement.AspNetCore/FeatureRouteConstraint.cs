// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A route constraint used to require a feature for a given route
    /// </summary>
    class FeatureRouteConstraint : IRouteConstraint
    {
        private readonly string _featureName;

        public FeatureRouteConstraint(string featureName)
        {
            if (string.IsNullOrEmpty(featureName))
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            _featureName = featureName;
        }

        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            return httpContext.RequestServices.GetRequiredService<IFeatureManagerSnapshot>().IsEnabled(_featureName);
        }
    }
}
