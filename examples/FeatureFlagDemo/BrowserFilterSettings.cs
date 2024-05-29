// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace FeatureFlagDemo.FeatureManagement.FeatureFilters
{
    public class BrowserFilterSettings
    {
        public IList<string> AllowedBrowsers { get; set; } = new List<string>();
    }
}
