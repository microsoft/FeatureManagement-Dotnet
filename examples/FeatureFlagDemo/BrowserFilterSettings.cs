// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using System.Collections.Generic;

namespace FeatureFlagDemo
{
	public class BrowserFilterSettings
    {
        public IEnumerable<string> AllowedBrowsers { get; } = new List<string>();
    }
}
