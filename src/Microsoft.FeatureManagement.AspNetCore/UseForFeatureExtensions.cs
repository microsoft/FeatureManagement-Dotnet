// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Extension methods that provide feature management integration for ASP.NET Core application building.
    /// </summary>
    public static class UseForFeatureExtensions
    {
        /// <summary>
        /// Conditionally creates a branch in the request pipeline that is rejoined to the main pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="featureName">The feature that is required to be enabled to take use this application branch</param>
        /// <param name="configuration">Configures a branch to take</param>
        /// <returns></returns>
        public static IApplicationBuilder UseForFeature(this IApplicationBuilder app, string featureName, Action<IApplicationBuilder> configuration)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (string.IsNullOrEmpty(featureName))
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            //
            // Create and configure the branch builder right away; otherwise,
            // we would end up running our branch after all the components
            // that were subsequently added to the main builder.
            IApplicationBuilder branchBuilder = app.New();

            configuration(branchBuilder);

            return app.Use(main =>
            {
                // This is called only when the main application builder 
                // is built, not per request.
                branchBuilder.Run(main);

                RequestDelegate branch = branchBuilder.Build();

                return async (context) =>
                {
                    IFeatureManager fm = context.RequestServices.GetRequiredService<IFeatureManagerSnapshot>();

                    if (await fm.IsEnabledAsync(featureName).ConfigureAwait(false))
                    {
                        await branch(context).ConfigureAwait(false);
                    }
                    else
                    {
                        await main(context).ConfigureAwait(false);
                    }
                };
            });
        }

        /// <summary>
        /// Conditionally creates a branch in the request pipeline that is rejoined to the main pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="featureName">Name of the feature that needs to be enabled to include the middleware in the application pipeline.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseMiddlewareForFeature<T>(this IApplicationBuilder app, string featureName)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (string.IsNullOrEmpty(featureName))
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            //
            // Create and configure the branch builder right away; otherwise,
            // we would end up running our branch after all the components
            // that were subsequently added to the main builder.
            IApplicationBuilder branchBuilder = app.New();

            return app.UseForFeature(featureName, builder =>
            {
                builder.UseMiddleware<T>();
            });
        }
    }
}
