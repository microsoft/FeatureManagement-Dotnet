// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace OpenTelemetryDemo.Pages
{
    public class RandomizeUserModel : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            // Generate new user claim
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Random.Shared.Next().ToString())
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("CookieAuth", principal);

            return RedirectToPage("/Index");
        }
    }
}
