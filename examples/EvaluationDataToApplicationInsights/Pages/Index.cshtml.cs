using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement;

namespace EvaluationDataToApplicationInsights.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IVariantFeatureManager _featureManager;
        private readonly TelemetryClient _telemetry;

        public string Username { get; set; }

        public IndexModel(
            IVariantFeatureManager featureManager,
            TelemetryClient telemetry)
        {
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        public async Task<IActionResult> OnGet()
        {
            Username = Request.Cookies["username"];

            if (string.IsNullOrEmpty(Username))
            {
                return Redirect("/RandomizeUser");
            }

            //
            // Use application's feature manager to get assigned variant for current user
            Variant variant = await _featureManager
                .GetVariantAsync("ImageRating", HttpContext.RequestAborted);

            //
            // Set the page's display image based on the assigned variant.
            ViewData["ImageUri"] = variant.Configuration.Value;

            return Page();
        }

        public IActionResult OnPost()
        {
            if (Request.Form != null)
            {
                string val = Request.Form["imageScore"];

                if (val != null && 
                    int.TryParse(val, out int rating))
                {
                    _telemetry.TrackEvent(
                        "Vote",
                        properties: null,
                        metrics: new Dictionary<string, double>
                        {
                            { "ImageRating", rating }
                        });
                }
            }

            return Redirect("/RandomizeUser");
        }
    }
}
