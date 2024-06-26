// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement.Mvc;
using System;
using System.Collections.Generic;

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
    }
}
