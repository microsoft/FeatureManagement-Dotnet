// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.FeatureFilters;
using System.Security.Claims;

namespace VariantsDemo
{
    /// <summary>
    /// Provides an implementation of <see cref="ITargetingContextAccessor"/> that creates a targeting context using info from the current HTTP request.
    /// </summary>
    public class HttpContextTargetingContextAccessor : ITargetingContextAccessor
    {
        private const string TargetingContextLookup = "HttpContextTargetingContextAccessor.TargetingContext";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextTargetingContextAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public ValueTask<TargetingContext> GetContextAsync()
        {
            HttpContext httpContext = _httpContextAccessor.HttpContext;

            // Generate a random user id for the request if one doesn't already exist
            if (httpContext.User?.Identity?.Name == null)
            {
                var random = new Random();
                int userId = random.Next();

                var identity = new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userId.ToString(), ClaimValueTypes.Integer32)
                }, "Randomized");

                httpContext.User = new ClaimsPrincipal(identity);
            }


            TargetingContext targetingContext = new TargetingContext
            {
                UserId = httpContext.User.Identity.Name
            };

            return new ValueTask<TargetingContext>(targetingContext);
        }
    }
}
