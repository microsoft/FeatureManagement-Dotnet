// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace Tests.FeatureManagement.Pages
{
    [FeatureGate(RequirementType.Not, Features.OffTestFeature)]
    public class RazorTestNotOffModel : PageModel
    {
        public IActionResult OnGet()
        {
            return new OkResult();
        }
    }
}
