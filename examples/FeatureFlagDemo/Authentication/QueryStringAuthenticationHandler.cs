// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace FeatureFlagDemo.Authentication
{
    /// <summary>
    /// This is a contrived authentication handler that authenticates a user based off of parameters passed in via the request's query string parameters.
    /// No secret exchange/verification is performed, so this handler should not be used in scenarios outside of this demo application.
    /// 
    /// To assign a user, use the following query string structure "?username=JohnDoe&groups=MyGroup1,MyGroup2"
    /// </summary>
    class QueryStringAuthenticationHandler : AuthenticationHandler<QueryStringAuthenticationOptions>
    {
        public QueryStringAuthenticationHandler(IOptionsMonitor<QueryStringAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity();

            //
            // Extract username
            if (Context.Request.Query.TryGetValue(Options.UsernameParameterName, out StringValues value))
            {
                string username = value.First();

                identity.AddClaim(new Claim(System.Security.Claims.ClaimTypes.Name, username));

                Logger.LogInformation($"Assigning the username '{username}' to the request.");
            }

            //
            // Extract groups
            if (Context.Request.Query.TryGetValue(Options.GroupsParameterName, out StringValues groupsValue))
            {
                IEnumerable<string> groups = groupsValue.First().Split(',').Select(g => g.Trim());

                foreach (string group in groups)
                {
                    identity.AddClaim(new Claim(ClaimTypes.GroupName, group));
                }

                Logger.LogInformation($"Assigning the following groups '{string.Join(", ", groups)}' to the request.");
            }

            //
            // Build principal and return result
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
