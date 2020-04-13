// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FeatureFlagDemo
{
    /// <summary>
    /// This is a contrived middleware used to assign user information to the request based off of parameters passed in via the request's query string parameters.
    /// Typically an application will authenticate a user and populate user information during that process.
    /// 
    /// To assign a user use the following query string structure "?username=JohnDoe&groups=MyGroup1,MyGroup2"
    /// </summary>
    public class AssignUserMiddleware
    {
        //
        // The middleware delegate to call after this one finishes processing
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public AssignUserMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<ThirdPartyMiddleware>();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var identity = new ClaimsIdentity();

            if (httpContext.Request.Query.TryGetValue("username", out StringValues value))
            {
                string username = value.First();

                identity.AddClaim(new Claim(System.Security.Claims.ClaimTypes.Name, username));

                _logger.LogInformation($"Assigning the username '{username}' to the request.");
            }

            if (httpContext.Request.Query.TryGetValue("groups", out StringValues groupsValue))
            {
                string[] groups = groupsValue.First().Split(',');

                foreach (string group in groups)
                {
                    identity.AddClaim(new Claim(ClaimTypes.GroupName, group));
                }
            }

            httpContext.User = new ClaimsPrincipal(identity);

            //
            // Call the next middleware delegate in the pipeline 
            await _next.Invoke(httpContext);
        }
    }
}
