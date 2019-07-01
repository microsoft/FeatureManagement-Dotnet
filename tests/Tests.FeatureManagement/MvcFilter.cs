// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc.Filters;

namespace Tests.FeatureManagement
{
    public class MvcFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            context.HttpContext.Response.Headers[nameof(MvcFilter)] = true.ToString();
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }
    }
}
