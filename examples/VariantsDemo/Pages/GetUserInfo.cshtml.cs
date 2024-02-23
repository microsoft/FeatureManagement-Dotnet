using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System.Text.Json;

namespace VariantsDemo.Pages
{
    public class GetUserInfoModel : PageModel
    {
        private readonly IVariantFeatureManager _featureManager;
        private readonly TelemetryClient _telemetry;

        private readonly string[] commonNames = { "John", "William", "Robert", "Thomas", "James", "David", "Charles", "Michael", "Peter", "Richard", "George", "Paul", "Joseph", "Johann", "Anna", "Henry", "Daniel", "Jan", "Elizabeth", "Edward", "Maria", "Mary", "Hans", "Karl", "Alexander", "Josef", "Martin", "Christian", "Jean", "Walter", "Andrew", "Arthur", "Antonio", "Carl", "Pierre", "Anne", "Friedrich", "Sarah", "Louis", "Albert", "Samuel", "Frank", "Margaret", "Mark", "Wilhelm", "Franz", "Patrick", "Heinrich", "Johannes", "Georg", "Juan", "Alfred", "Christopher", "José", "Francis", "Carlos", "Marie", "Stephen", "Adam", "Aleksandr", "Laura", "Francisco", "Rudolf", "Otto", "Catherine", "Manuel", "Barbara", "Ernst", "František", "Eric", "Benjamin", "Andreas", "Anthony", "Simon", "Hermann", "Matthew", "Jacques", "Max", "Luis", "Frederick", "François", "Jane", "Giovanni", "Karel", "Tom", "Vladimir", "Brian", "Anton", "Jonathan", "Eva", "Francesco", "Giuseppe", "Philip", "Stefan", "Harry", "Ana", "Michel", "Johan", "Pedro", "André" };

        public GetUserInfoModel(
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
            public string VariantName { get; set; }
            public string Variant { get; set; }
        }

        private async Task<UserInfo> GenerateRandomUserInfo()
        {
            var random = new Random();
            int userId = random.Next();

            TargetingContext targetingContext = new TargetingContext
            {
                UserId = userId.ToString()
            };

            Variant variant = await _featureManager.GetVariantAsync("Algorithm", targetingContext, CancellationToken.None);

            return new UserInfo
            {
                Username = commonNames[Decimal.ToInt32(100 * (decimal)userId / Int32.MaxValue)],
                VariantName = variant.Name,
                Variant = variant.Configuration.Get<string>()
            };
        }
    }
}
