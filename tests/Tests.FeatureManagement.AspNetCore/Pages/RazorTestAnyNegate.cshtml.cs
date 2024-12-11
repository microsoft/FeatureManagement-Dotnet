// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace Tests.FeatureManagement.AspNetCore.Pages
{
    [FeatureGate(requirementType: RequirementType.Any, negate: true, Features.ConditionalFeature, Features.ConditionalFeature2)]
    public class RazorTestAnyNegateModel : PageModel
    {
        public IActionResult OnGet()
        {
            return new OkResult();
        }
    }
}
