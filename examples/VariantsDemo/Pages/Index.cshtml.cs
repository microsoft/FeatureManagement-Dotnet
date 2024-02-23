using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement;
using System.Text.Json;
using Microsoft.ApplicationInsights;

namespace VariantsDemo.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IVariantFeatureManager _featureManager;
        private readonly TelemetryClient _telemetry;

        public IndexModel(
            IVariantFeatureManager featureManager,
            TelemetryClient telemetry)
        {
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        public async Task<IActionResult> OnGet()
        {
            return Page();
        }
    }
}
