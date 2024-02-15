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
            UserInfo info = await GenerateRandomUserInfo();

            string result = JsonSerializer.Serialize<UserInfo>(info);

            return Content(result, "application/json");
        }

        public class UserInfo
        {
            public string Username { get; set; }
            public string Variant { get; set; }
        }

        private async Task<UserInfo> GenerateRandomUserInfo()
        {
            var random = new Random();
            var username = random.Next().ToString();

            TargetingContext targetingContext = new TargetingContext
            {
                UserId = username
            };

            Variant variant = await _featureManager.GetVariantAsync("Algorithm", targetingContext, CancellationToken.None);

            return new UserInfo
            {
                Username = username,
                Variant = variant.Configuration.Get<string>()
            };
        }
    }
}
