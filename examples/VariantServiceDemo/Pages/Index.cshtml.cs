using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement;
using System.Security.Claims;

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
            //
            // generate a new visitor
            Username = Random.Shared.Next().ToString();

            // Clear Application Insights cookies and 
            Response.Cookies.Delete("ai_user");
            Response.Cookies.Delete("ai_session");

            // Generate new user claim
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Username)
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            HttpContext.SignInAsync("CookieAuth", principal);

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
