// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a way to add MVC routes for a given feature.
    /// </summary>
    public static class RouteBuilderExtensions
    {
        /// <summary>
        ///  Maps an MVC route that is only used if the given feature is enabled.
        /// </summary>
        /// <param name="routeBuilder">The <see cref="Microsoft.AspNetCore.Routing.IRouteBuilder"/> to add the route to.</param>
        /// <param name="featureName">The feature requried to activate the rout.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="template">The URL pattern of the route.</param>
        /// <param name="defaults">An object that contains default values for route parameters. The object's properties represent the names and values of the default values.</param>
        /// <param name="constraints">An object that contains constraints for the route. The object's properties represent the names and values of the constraints.</param>
        /// <param name="dataTokens">An object that contains data tokens for the route. The object's properties represent the names and values of the data tokens.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>  
        public static IRouteBuilder MapRouteForFeature(this IRouteBuilder routeBuilder, string featureName, string name, string template, object defaults, object constraints, object dataTokens)
        {
            routeBuilder.MapRoute(
                name: name,
                template: template,
                defaults: defaults,
                constraints: new { featureConstraints = new FeatureRouteConstraint(featureName) },
                dataTokens: dataTokens);

            return routeBuilder;
        }
    }
}
