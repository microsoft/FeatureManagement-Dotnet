using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace VariantAndAzureMonitorDemo.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IVariantFeatureManager _featureManager;
        private readonly Meter _meter;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IVariantFeatureManager featureManager, IMeterFactory meterFactory, ILogger<IndexModel> logger)
        {
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
            _meter = meterFactory?.Create("VariantAndAzureMonitorDemo") ?? throw new ArgumentNullException(nameof(meterFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Username { get; set; }

        public async Task<IActionResult> OnGet()
        {
            Username = HttpContext.User.Identity.Name;

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
                    // Create a histogram to track the image rating
                    var imageRatingHistogram = _meter.CreateHistogram<long>("ImageRating");
                    imageRatingHistogram.Record(rating);

                    // Track the vote event using ILogger custom event
                    _logger.LogVote();
                }
            }

            return Redirect("/RandomizeUser");
        }
    }
}
