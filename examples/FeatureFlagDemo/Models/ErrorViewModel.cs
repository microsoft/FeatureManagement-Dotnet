// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

using System;

namespace FeatureFlagDemo.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
