// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace TargetingConsoleApp.Identity
{
    interface IUserRepository
    {
        Task<User> GetUser(string id);
    }
}
