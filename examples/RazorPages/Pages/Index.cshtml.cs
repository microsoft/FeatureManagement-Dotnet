// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement.Mvc;

namespace RazorPages.Pages
{
    [FeatureGate("Home")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
