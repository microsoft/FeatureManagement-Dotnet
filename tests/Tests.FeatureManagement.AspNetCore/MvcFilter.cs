// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace Tests.FeatureManagement.AspNetCore
{
    public class MvcFilter : IAsyncActionFilter
    {
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            context.HttpContext.Response.Headers[nameof(MvcFilter)] = bool.TrueString;

            return next();
        }
    }
}
