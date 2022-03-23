// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement.Mvc;

namespace Tests.FeatureManagement.Pages
{
    [FeatureGate(Features.ConditionalFeature, Features.ConditionalFeature2)]
    public class RazorTestAllModel : PageModel
    {
        public IActionResult OnGet()
        {
            return StatusCode(200);
        }
    }
}
