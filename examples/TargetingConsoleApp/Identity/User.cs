// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using System.Collections.Generic;

namespace Consoto.Banking.AccountService.Identity
{
    internal class User
    {
        public string Id { get; set; }

        public IEnumerable<string> Groups { get; set; }
    }
}
