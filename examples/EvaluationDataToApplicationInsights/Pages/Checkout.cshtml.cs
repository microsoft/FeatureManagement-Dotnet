using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EvaluationDataToApplicationInsights.Pages
{
    public class CheckoutModel : PageModel
    {
        private readonly ILogger<CheckoutModel> _logger;

        public CheckoutModel(ILogger<CheckoutModel> logger)
        {
            _logger = logger;
        }
    }
}