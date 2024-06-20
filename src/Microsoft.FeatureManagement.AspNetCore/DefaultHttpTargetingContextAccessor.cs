// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a default implementation of <see cref="ITargetingContextAccessor"/> that creates <see cref="TargetingContext"/> using info from the current HTTP request.
    /// </summary>
    public sealed class DefaultHttpTargetingContextAccessor : ITargetingContextAccessor
    {
        /// <summary>
        /// The key used to store and retrieve the <see cref="TargetingContext"/> from the <see cref="HttpContext"/> items.
        /// </summary>
        private static object _cacheKey = new object();

        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Creates an instance of the DefaultHttpTargetingContextAccessor
        /// </summary>
        public DefaultHttpTargetingContextAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Gets <see cref="TargetingContext"/> from the current HTTP request.
        /// </summary>
        public ValueTask<TargetingContext> GetContextAsync()
        {
            HttpContext httpContext = _httpContextAccessor.HttpContext;

            //
            // Try cache lookup
            if (httpContext.Items.TryGetValue(_cacheKey, out object value))
            {
                return new ValueTask<TargetingContext>((TargetingContext)value);
            }

            //
            // Treat user identity name as user id
            ClaimsPrincipal user = httpContext.User;

            string userId = user?.Identity?.Name;

            //
            // Treat claims of type Role as groups
            IEnumerable<string> groups = httpContext.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value);

            TargetingContext targetingContext = new TargetingContext
            {
                UserId = userId,
                Groups = groups
            };

            //
            // Cache for subsequent lookup
            httpContext.Items[_cacheKey] = targetingContext;

            return new ValueTask<TargetingContext>(targetingContext);
        }
    }
}