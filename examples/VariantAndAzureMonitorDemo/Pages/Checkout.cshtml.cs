using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace VariantAndAzureMonitorDemo.Pages
{
    public class CheckoutModel : PageModel
    {
        private readonly Meter _meter;
        private readonly ILogger<CheckoutModel> _logger;

        public CheckoutModel(IMeterFactory meterFactory, ILogger<CheckoutModel> logger)
        {
            _meter = meterFactory?.Create("VariantAndAzureMonitorDemo") ?? throw new ArgumentNullException(nameof(meterFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActionResult OnPost()
        {
            // Track the checkout event using ILogger custom event
            _logger.LogInformation("{microsoft.custom_event.name} {success}", "checkout", "yes");

            // Track the checkout amount metric
            var checkoutAmountHistogram = _meter.CreateHistogram<long>("checkoutAmount");
            checkoutAmountHistogram.Record(Random.Shared.Next(1, 100));

            TempData["CheckedOut"] = true;

            return Page();
        }
    }
}
