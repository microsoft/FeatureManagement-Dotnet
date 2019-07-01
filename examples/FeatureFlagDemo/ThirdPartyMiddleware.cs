// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FeatureFlagDemo
{
    public class ThirdPartyMiddleware
    {
        //
        // The middleware delegate to call after this one finishes processing
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public ThirdPartyMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<ThirdPartyMiddleware>();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            _logger.LogInformation($"Third party middleware inward path.");

            //
            // Call the next middleware delegate in the pipeline 
            await _next.Invoke(httpContext);

            _logger.LogInformation($"Third party middleware outward path.");
        }
    }
}
