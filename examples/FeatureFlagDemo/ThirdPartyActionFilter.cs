// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace FeatureFlagDemo
{
    public class ThirdPartyActionFilter : IActionFilter
    {
        private ILogger _logger;

        public ThirdPartyActionFilter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ThirdPartyActionFilter>();
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            _logger.LogInformation("Third party action filter inward path.");
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            _logger.LogInformation("Third party action filter outward path.");
        }
    }
}
