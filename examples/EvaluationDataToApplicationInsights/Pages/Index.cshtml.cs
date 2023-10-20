using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement;

namespace EvaluationDataToApplicationInsights.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IFeatureManager _featureManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public string Username { get; set; }

        public IndexModel(ILogger<IndexModel> logger, IFeatureManager featureManager, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _featureManager = featureManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public IActionResult OnGet()
        {
            Username = _httpContextAccessor.HttpContext.Request.Cookies["username"];

            return Page();
        }
    }
}