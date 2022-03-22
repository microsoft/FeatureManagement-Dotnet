using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc.RazorPages;

namespace Tests.FeatureManagement
{
    [PageFeatureGate(Features.ConditionalFeature, Features.ConditionalFeature2)]
    public class RazorTestAllModel : PageModel
    {
        public IActionResult OnGet()
        {
            return StatusCode(200);
        }
    }

    [PageFeatureGate(RequirementType.Any, Features.ConditionalFeature, Features.ConditionalFeature2)]
    public class RazorTestAnyModel : PageModel
    {
        public IActionResult OnGet()
        {
            return StatusCode(200);
        }
    }
}
