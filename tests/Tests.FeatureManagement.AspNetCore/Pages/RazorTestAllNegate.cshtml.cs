// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement.Mvc;

namespace Tests.FeatureManagement.AspNetCore.Pages
{
    [FeatureGate(negate: true, Features.ConditionalFeature, Features.ConditionalFeature2)]
    public class RazorTestAllNegateModel : PageModel
    {
        public IActionResult OnGet()
        {
            return new OkResult();
        }
    }
}
