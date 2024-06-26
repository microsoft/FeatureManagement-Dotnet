// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Mvc
{
    /// <summary>
    /// A handler that is invoked when an MVC action requires a feature and the feature is not enabled.
    /// </summary>
    public interface IDisabledFeaturesHandler
    {
        /// <summary>
        /// Callback used to handle requests to an MVC action that require a feature that is disabled.
        /// </summary>
        /// <param name="features">The name of the features that the action could have been activated for.</param>
        /// <param name="context">The action executing context provided by MVC.</param>
        /// <returns>The task.</returns>
        Task HandleDisabledFeatures(IEnumerable<string> features, ActionExecutingContext context);
    }
}
