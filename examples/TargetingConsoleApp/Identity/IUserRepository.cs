// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading.Tasks;

namespace Consoto.Banking.AccountService.Identity
{
	internal interface IUserRepository
    {
        Task<User> GetUser(string id);
    }
}
