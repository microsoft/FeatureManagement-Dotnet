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
        private readonly IFeatureManager _featureManager;
        private readonly IFeatureVariantManager _variantManager;

        public HomeController(IFeatureManagerSnapshot featureSnapshot,
                              IFeatureVariantManager variantManager)
        {
            _featureManager = featureSnapshot;
            _variantManager = variantManager;
        }

        [FeatureGate(MyFeatureFlags.Home)]
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> About(CancellationToken cancellationToken)
        {
            ViewData["Message"] = "Your application description page.";

            if (await _featureManager.IsEnabledAsync(MyFeatureFlags.CustomViewData, cancellationToken))
            {
                ViewData["Message"] = $"This is FANCY CONTENT you can see only if '{MyFeatureFlags.CustomViewData}' is enabled.";
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
