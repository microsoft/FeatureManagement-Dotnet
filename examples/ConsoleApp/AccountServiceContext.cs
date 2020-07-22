// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Consoto.Banking.AccountService.FeatureFilters;

namespace Consoto.Banking.AccountService
{
    class AccountServiceContext : IAccountContext
    {
        public string AccountId { get; set; }
        
        public string ID { get; private set; }

        /// <summary>
        /// Creates feature context.
        /// </summary>
        /// <param name="id">
        /// Feature context identifier.
        /// Optional if contexts are not required to be unique.
        /// </param>
        public AccountServiceContext(string id = null)
        {
            ID = id;
        }
    }
}
