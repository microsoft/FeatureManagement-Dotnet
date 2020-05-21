// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace FeatureFlagDemo.Controllers
{
	public class BetaController : Controller
	{
		[FeatureGate(MyFeatureFlags.Beta)]
		public IActionResult Index()
		{
			return View();
		}
	}
}
