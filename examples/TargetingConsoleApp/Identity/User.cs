// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;

namespace TargetingConsoleApp.Identity
{
    class User
    {
        public string Id { get; set; }

        public IEnumerable<string> Groups { get; set; }
    }
}
