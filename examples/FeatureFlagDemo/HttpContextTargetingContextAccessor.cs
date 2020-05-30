// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement.FeatureFilters;

namespace FeatureFlagDemo
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
            var httpContext = _httpContextAccessor.HttpContext;

            //
            // Try cache lookup
            if (httpContext.Items.TryGetValue(TargetingContextLookup, out var value))
            {
                return new ValueTask<TargetingContext>((TargetingContext)value);
            }

            var user = httpContext.User;

            var groups = new List<string>();

            //
            // This application expects groups to be specified in the user's claims
            foreach (var claim in user.Claims)
            {
                if (claim.Type == ClaimTypes.GroupName)
                {
                    groups.Add(claim.Value);
                }
            }

            //
            // Build targeting context based off user info
            var targetingContext = new TargetingContext {UserId = user.Identity.Name, Groups = groups};

            //
            // Cache for subsequent lookup
            httpContext.Items[TargetingContextLookup] = targetingContext;

            return new ValueTask<TargetingContext>(targetingContext);
        }
    }
}
