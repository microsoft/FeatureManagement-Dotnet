using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BlazorServerApp
{
    public class RandomAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            int randomNum = Random.Shared.Next();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, randomNum.ToString())
            };

            ClaimsIdentity claimsIdentity;

            if (randomNum % 2 == 0)
            {
                //
                // If the random number is even, the user is considered as authenticated.
                claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            }
            else
            {
                //
                // If the random number is odd, the user is considered as not authenticated.
                claimsIdentity = new ClaimsIdentity(claims);
            }

            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var state = new AuthenticationState(claimsPrincipal);

            NotifyAuthenticationStateChanged(Task.FromResult(state));

            return Task.FromResult(state);
        }
    }
}
