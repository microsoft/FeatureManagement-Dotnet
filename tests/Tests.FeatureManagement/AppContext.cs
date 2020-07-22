// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Tests.FeatureManagement
{
    class AppContext : IAccountContext
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
        public AppContext(string id = null)
        {
            ID = id;
        }
    }
}
