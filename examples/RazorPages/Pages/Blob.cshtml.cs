using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Mvc.RazorPages;

namespace RazorPages.Pages
{
    [PageFeatureGate("Home")]
    public class BlobModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
