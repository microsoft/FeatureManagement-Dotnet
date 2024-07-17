// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace FeatureFlagDemo.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
