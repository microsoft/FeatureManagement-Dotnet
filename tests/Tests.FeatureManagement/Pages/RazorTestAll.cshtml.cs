using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement.Mvc.RazorPages;

namespace Tests.FeatureManagement.Pages
{
    [PageFeatureGate(Features.ConditionalFeature, Features.ConditionalFeature2)]
    public class RazorTestAllModel : PageModel
    {
        public IActionResult OnGet()
        {
            return StatusCode(200);
        }
    }
}
