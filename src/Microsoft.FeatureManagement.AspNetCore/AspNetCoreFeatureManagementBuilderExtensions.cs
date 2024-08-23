// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Extensions to the <see cref="IFeatureManagementBuilder"/>.
    /// </summary>
    public static class AspNetCoreFeatureManagementBuilderExtensions
    {
        /// <summary>
        /// Registers a disabled feature handler. This will be invoked for MVC actions that require a feature that is not enabled.
        /// </summary>
        /// <param name="builder">The feature management builder.</param>
        /// <param name="disabledFeaturesHandler">The disabled feature handler.</param>
        /// <returns>The feature management builder.</returns>
        public static IFeatureManagementBuilder UseDisabledFeaturesHandler(this IFeatureManagementBuilder builder, IDisabledFeaturesHandler disabledFeaturesHandler)
        {
            builder.Services.AddSingleton<IDisabledFeaturesHandler>(disabledFeaturesHandler ?? throw new ArgumentNullException(nameof(disabledFeaturesHandler)));

            return builder;
        }

        /// <summary>
        /// Provides a way to specify an inline disabled feature handler.
        /// </summary>
        /// <param name="builder">The feature management builder.</param>
        /// <param name="handler">The inline handler for disabled features.</param>
        /// <returns>The feature management builder.</returns>
        public static IFeatureManagementBuilder UseDisabledFeaturesHandler(this IFeatureManagementBuilder builder, Action<IEnumerable<string>, ActionExecutingContext> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            builder.UseDisabledFeaturesHandler(new InlineDisabledFeaturesHandler(handler));

            return builder;
        }

        /// <summary>
        /// Enables the use of targeting within the application and adds a targeting context accessor that extracts targeting details from a request's HTTP context.
        /// </summary>
        /// <param name="builder">The <see cref="IFeatureManagementBuilder"/> used to customize feature management functionality.</param>
        /// <returns>A <see cref="IFeatureManagementBuilder"/> that can be used to customize feature management functionality.</returns>
        public static IFeatureManagementBuilder WithTargeting(this IFeatureManagementBuilder builder)
        {
            // Add HttpContextAccessor if it doesn't already exist
            if (!builder.Services.Any(service => service.ServiceType == typeof(IHttpContextAccessor)))
            {
                builder.Services.AddHttpContextAccessor();
            }

            //
            // Register the targeting context accessor with the same lifetime as the feature manager
            if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IFeatureManager) && descriptor.Lifetime == ServiceLifetime.Scoped))
            {
                builder.Services.TryAddScoped<ITargetingContextAccessor, DefaultHttpTargetingContextAccessor>();
            }
            else
            {
                builder.Services.TryAddSingleton<ITargetingContextAccessor, DefaultHttpTargetingContextAccessor>();
            }

            builder.AddFeatureFilter<TargetingFilter>();

            return builder;
        }
    }
}
