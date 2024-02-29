using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace VariantsDemo.Pages
{
    [IgnoreAntiforgeryToken]
    public class GetUserInfoModel : PageModel
    {
        private readonly IVariantFeatureManager _featureManager;
        private readonly TelemetryClient _telemetry;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private const string _eventName = "FeatureEvaluation";
        private readonly string[] commonNames = { "John", "William", "Robert", "Thomas", "James", "David", "Charles", "Michael", "Peter", "Richard", "George", "Paul", "Joseph", "Johann", "Anna", "Henry", "Daniel", "Jan", "Elizabeth", "Edward", "Maria", "Mary", "Hans", "Karl", "Alexander", "Josef", "Martin", "Christian", "Jean", "Walter", "Andrew", "Arthur", "Antonio", "Carl", "Pierre", "Anne", "Friedrich", "Sarah", "Louis", "Albert", "Samuel", "Frank", "Margaret", "Mark", "Wilhelm", "Franz", "Patrick", "Heinrich", "Johannes", "Georgina", "Juan", "Alfred", "Christopher", "José", "Francis", "Carlos", "Marie", "Stephen", "Adam", "Aleksandr", "Laura", "Francisco", "Rudolf", "Otto", "Catherine", "Manuel", "Barbara", "Ernst", "František", "Eric", "Benjamin", "Andreas", "Anthony", "Simon", "Hermann", "Matthew", "Jacques", "Max", "Luis", "Frederick", "François", "Jane", "Giovanni", "Karel", "Tom", "Vladimir", "Brian", "Anton", "Jonathan", "Eva", "Francesco", "Giuseppe", "Philip", "Stefan", "Harry", "Ana", "Michel", "Johan", "Pedro", "André" };

        public GetUserInfoModel(
            IVariantFeatureManager featureManager,
            TelemetryClient telemetry,
            IHttpContextAccessor httpContextAccessor)
        {
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task<IActionResult> OnPostAsync([FromBody] NewUserRequest newUserRequest)
        {
            if (newUserRequest?.UserInfo != null)
            {
                _telemetry.TrackEvent("Finished", new Dictionary<string, string> { { "TargetingId", newUserRequest.UserInfo.UserId.ToString() } }, new Dictionary<string, double> { { "Duration", newUserRequest.Duration } });
            }

            UserInfo info = await GenerateRandomUserInfo();

            string result = JsonSerializer.Serialize<UserInfo>(info);

            return Content(result, "application/json");
        }

        public class NewUserRequest
        {
            public UserInfo UserInfo { get; set; }
            public int Duration { get; set; }
        }

        public class UserInfo
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public string VariantName { get; set; }
            public int Variant { get; set; }
        }

        private async Task<UserInfo> GenerateRandomUserInfo()
        {
            Variant variant = await _featureManager.GetVariantAsync("WorkerConfiguration", CancellationToken.None);

            HttpContext httpContext = _httpContextAccessor.HttpContext;

            return new UserInfo
            {
                UserId = int.Parse(httpContext.User.Identity.Name),
                Username = commonNames[Decimal.ToInt32(100 * decimal.Parse(httpContext.User.Identity.Name) / Int32.MaxValue)],
                VariantName = variant.Name,
                Variant = variant.Configuration.Get<int>()
            };
        }
    }
}
