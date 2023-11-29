using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EvaluationDataToApplicationInsights.Pages
{
    public class RandomizeUserModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Clear Application Insights cookies and generate new username
            Response.Cookies.Delete("ai_user");
            Response.Cookies.Delete("ai_session");
            Response.Cookies.Append("username", Random.Shared.Next().ToString());

            return RedirectToPage("/Index");
        }
    }
}
