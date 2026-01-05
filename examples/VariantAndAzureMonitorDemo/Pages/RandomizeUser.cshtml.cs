using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace VariantAndAzureMonitorDemo.Pages
{
    public class RandomizeUserModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Generate new user claim
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Random.Shared.Next().ToString())
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            HttpContext.SignInAsync("CookieAuth", principal);

            return RedirectToPage("/Index");
        }
    }
}
