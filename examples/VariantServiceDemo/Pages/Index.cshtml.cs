using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement;

namespace VariantServiceDemo.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IVariantServiceProvider<ICalculator> _calculatorProvider;

        public string Username { get; set; }

        public IndexModel(IVariantServiceProvider<ICalculator> calculatorProvider)
        {
            _calculatorProvider = calculatorProvider ?? throw new ArgumentNullException(nameof(calculatorProvider));
        }

        public IActionResult OnGet()
        {
            Response.Cookies.Delete("ai_user");

            Response.Cookies.Delete("ai_session");

            //
            // generate a new visitor
            string visitor = Random.Shared.Next().ToString();

            Response.Cookies.Append("username", visitor);

            Username = visitor;

            return Page();
        }

        public async Task<JsonResult> OnGetCalculate(double a, double b)
        {
            ICalculator calculator = await _calculatorProvider.GetServiceAsync(HttpContext.RequestAborted);

            double result = await calculator.AddAsync(a, b);

            return new JsonResult(result);
        }
    }
}
