// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Mvc.RazorPages;

namespace RazorPages.Pages
{
    [PageFeatureGate("Home")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
