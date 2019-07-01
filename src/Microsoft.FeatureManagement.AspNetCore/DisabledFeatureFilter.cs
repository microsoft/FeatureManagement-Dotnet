// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// An MVC filter that will run in place of any filter that requires a feature that is disabled to be enabled.
    /// </summary>
    class DisabledFeatureFilter : IActionFilter
    {
        public DisabledFeatureFilter(string featureName)
        {
            FeatureName = featureName;
        }

        public string FeatureName { get; }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }
    }
}
