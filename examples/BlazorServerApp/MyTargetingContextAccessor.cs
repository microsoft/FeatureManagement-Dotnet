using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.FeatureManagement.FeatureFilters;

namespace BlazorServerApp
{
    public class MyTargetingContextAccessor : ITargetingContextAccessor
    {
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public MyTargetingContextAccessor(AuthenticationStateProvider authenticationStateProvider)
        {
            _authenticationStateProvider = authenticationStateProvider ?? throw new ArgumentNullException(nameof(authenticationStateProvider));
        }

        public async ValueTask<TargetingContext> GetContextAsync()
        {
            AuthenticationState authState = await _authenticationStateProvider.GetAuthenticationStateAsync();

            string username = authState.User.Identity.Name;

            bool isAuthenticated = authState.User.Identity.IsAuthenticated;

            var groups = new List<string>();

            if (!isAuthenticated)
            {
                groups.Add("Guests");
            }

            var targetingContext = new TargetingContext
            {
                UserId = username,
                Groups = groups
            };

            return targetingContext;
        }
    }
}
