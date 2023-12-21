using BlazorServerApp.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement.FeatureFilters;
using System.Text.RegularExpressions;

namespace BlazorServerApp
{
    public class MyTargetingContextAccessor : ITargetingContextAccessor
    {
        private readonly UserAgentContextProvider _userAgentContextProvider;
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public MyTargetingContextAccessor(UserAgentContextProvider userAgentContextProvider, AuthenticationStateProvider authenticationStateProvider)
        {
            _userAgentContextProvider = userAgentContextProvider ?? throw new ArgumentNullException(nameof(userAgentContextProvider));
            _authenticationStateProvider = authenticationStateProvider ?? throw new ArgumentNullException(nameof(authenticationStateProvider));
        }

        public async ValueTask<TargetingContext> GetContextAsync()
        {
            AuthenticationState authState = await _authenticationStateProvider.GetAuthenticationStateAsync();

            string username = authState.User.Identity.Name;

            bool IsAuthenticated = authState.User.Identity.IsAuthenticated;

            string userAgentContext = _userAgentContextProvider.Context;

            var groups = new List<string>();

            if (!IsAuthenticated)
            {
                groups.Add("Guests");
            }

            if (IsEdgeBrowser(userAgentContext))
            {
                groups.Add("Edge");
            }

            if (IsFirefoxBrowser(userAgentContext))
            {
                groups.Add("Firefox");
            }

            TargetingContext targetingContext = new TargetingContext
            {
                UserId = username,
                Groups = groups
            };

            return targetingContext;
        }

        private static bool IsEdgeBrowser(string userAgentContext)
        {
            if (userAgentContext == null)
            {
                return false;
            }

            return userAgentContext.Contains("edge", StringComparison.OrdinalIgnoreCase) || userAgentContext.Contains("edg", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFirefoxBrowser(string userAgentContext)
        {
            if (userAgentContext == null)
            {
                return false;
            }

            return userAgentContext.Contains("Firefox", StringComparison.OrdinalIgnoreCase);
        }
    }
}
