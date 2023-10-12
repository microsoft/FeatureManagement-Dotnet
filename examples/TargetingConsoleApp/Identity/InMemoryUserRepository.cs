// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TargetingConsoleApp.Identity
{
    class InMemoryUserRepository : IUserRepository
    {
        public static readonly IEnumerable<User> Users = new User[]
        {
            new User
            {
                Id = "Jeff",
                Groups = Enumerable.Empty<string>()
            },
            new User
            {
                Id = "Alicia",
                Groups = Enumerable.Empty<string>()
            },
            new User
            {
                Id = "Susan",
                Groups = Enumerable.Empty<string>()
            },
            new User
            {
                Id = "JohnDoe",
                Groups = new List<string>()
                {
                    "Management"
                }
            },
            new User
            {
                Id = "JaneDoe",
                Groups = new List<string>()
                {
                    "Management"
                }
            },
            new User
            {
                Id = "Tim",
                Groups = new List<string>()
                {
                    "TeamMembers"
                }
            },
            new User
            {
                Id = "Tanya",
                Groups = new List<string>()
                {
                    "TeamMembers"
                }
            },
            new User
            {
                Id = "Alec",
                Groups = new List<string>()
                {
                    "TeamMembers"
                }
            },
            new User
            {
                Id = "Betty",
                Groups = new List<string>()
                {
                    "TeamMembers"
                }
            },
            new User
            {
                Id = "Anne",
                Groups = Enumerable.Empty<string>()
            },
            new User
            {
                Id = "Chuck",
                Groups =  new List<string>()
                {
                    "Contractor"
                }
            },
        };

        public Task<User> GetUser(string id)
        {
            return Task.FromResult(Users.FirstOrDefault(user => user.Id.Equals(id)));
        }
    }
}
