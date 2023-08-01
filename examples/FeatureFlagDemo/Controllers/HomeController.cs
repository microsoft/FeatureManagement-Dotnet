// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FeatureFlagDemo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using System.Threading.Tasks;
using System.Threading;

namespace FeatureFlagDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly IVariantFeatureManager _featureManager;

        public HomeController(IVariantFeatureManagerSnapshot featureSnapshot)
        {
            _featureManager = featureSnapshot;
        }

        [FeatureGate(MyFeatureFlags.Home)]
        public async Task<IActionResult> Index()
        {
            Variant test = await _featureManager.GetVariantAsync(nameof(MyFeatureFlags.Banner), CancellationToken.None);
            string x = test.Configuration["Size"];
            string y = test.Configuration.Value;
            bool isEnabled = await _featureManager.IsEnabledAsync(nameof(MyFeatureFlags.Banner), CancellationToken.None);
            return View();
        }

        public async Task<IActionResult> About(CancellationToken cancellationToken)
        {
            ViewData["Message"] = "Your application description page.";

            if (await _featureManager.IsEnabledAsync(nameof(MyFeatureFlags.CustomViewData), cancellationToken))
            {
                ViewData["Message"] = $"This is FANCY CONTENT you can see only if '{nameof(MyFeatureFlags.CustomViewData)}' is enabled.";
            };

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        [FeatureGate(MyFeatureFlags.Beta)]
        public IActionResult Beta()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
