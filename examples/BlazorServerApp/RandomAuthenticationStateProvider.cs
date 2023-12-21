using BlazorServerApp.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;

namespace BlazorServerApp
{
    public class RandomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedSessionStorage _sessionStorage;

        private AuthenticationState _state;

        public RandomAuthenticationStateProvider(ProtectedSessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_state == null)
            {
                var res = await _sessionStorage.GetAsync<User>("username");

                if (res.Success && res.Value != null)
                {
                    _state = GenerateState(res.Value.Name, res.Value.IsAuthenticated);

                    NotifyAuthenticationStateChanged(Task.FromResult(_state));
                }
                else
                {
                    int randomNum = Random.Shared.Next();

                    string username = randomNum.ToString();

                    //
                    // The random user has 2/3 chance to be authorized.
                    bool isAuthorized = randomNum % 3 != 0;

                    _state = GenerateState(username, isAuthorized);

                    NotifyAuthenticationStateChanged(Task.FromResult(_state));

                    await _sessionStorage.SetAsync(username, isAuthorized);
                }
            }

            return _state;
        }

        private static AuthenticationState GenerateState(string username, bool isAuthorized)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username)
            };

            ClaimsIdentity claimsIdentity;

            if (isAuthorized)
            {
                claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            }
            else
            {
                claimsIdentity = new ClaimsIdentity(claims);
            }

            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            return new AuthenticationState(claimsPrincipal);
        }
    }
}
