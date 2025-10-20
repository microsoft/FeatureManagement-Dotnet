using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace VariantAndTelemetryDemo.Pages
{
    public class RandomizeUserModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Clear Application Insights cookies and 
            Response.Cookies.Delete("ai_user");
            Response.Cookies.Delete("ai_session");

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
