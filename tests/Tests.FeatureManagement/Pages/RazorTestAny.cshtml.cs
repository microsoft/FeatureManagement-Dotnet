// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc.RazorPages;

namespace Tests.FeatureManagement.Pages
{
    [PageFeatureGate(RequirementType.Any, Features.ConditionalFeature, Features.ConditionalFeature2)]
    public class RazorTestAnyModel : PageModel
    {
        public IActionResult OnGet()
        {
            return StatusCode(200);
        }
    }
}
