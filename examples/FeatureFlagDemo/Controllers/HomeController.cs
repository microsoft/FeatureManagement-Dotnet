// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using FeatureFlagDemo.Models;

namespace FeatureFlagDemo.Controllers
{
	public class HomeController : Controller
	{
		private readonly IFeatureManager _featureManager;

		public HomeController(IFeatureManager featureSnapshot)
		{
			_featureManager = featureSnapshot;
		}

		[FeatureGate(MyFeatureFlags.Home)]
		public IActionResult Index()
		{
			return View();
		}

		public async Task<IActionResult> About()
		{
			ViewData["Message"] = "Your application description page.";

			if (await _featureManager.IsEnabledAsync(nameof(MyFeatureFlags.CustomViewData)))
			{
				ViewData["Message"] = "This is FANCY CONTENT you can see only if " +
				                      $"'{nameof(MyFeatureFlags.CustomViewData)}' is enabled.";
			}

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
			return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
		}
	}
}
