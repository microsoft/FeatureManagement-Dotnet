// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc.Filters;

namespace FeatureFlagDemo
{
    public class ThirdPartyActionFilter : IAsyncActionFilter
    {
        private ILogger _logger;

        public ThirdPartyActionFilter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ThirdPartyActionFilter>();
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            _logger.LogInformation("Third party action filter inward path.");

            await next().ConfigureAwait(false);

            _logger.LogInformation("Third party action filter outward path.");
        }
    }
}
