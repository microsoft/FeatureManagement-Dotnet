// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

using System.Threading.Tasks;

namespace TargetingConsoleApp.Identity
{
    interface IUserRepository
    {
        Task<User> GetUser(string id);
    }
}
