// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// Contextual information that is required to perform a targeting evaluation.
    /// </summary>
    public class TargetingContext : ITargetingContext
    {
        /// <summary>
        /// The user id that should be considered when evaluating if the context is being targeted.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The groups that should be considered when evaluating if the context is being targeted.
        /// </summary>
        public IEnumerable<string> Groups { get; set; }

        /// <summary>
        /// Context ID.
        /// Used to determine uniqueness of a context.
        /// Generated and provided by the caller. 
        /// </summary>
        public string ID { get; private set; }

        /// <summary>
        /// Creates feature context.
        /// </summary>
        /// <param name="id">
        /// Feature context identifier.
        /// Optional if contexts are not required to be unique.
        /// </param>
        public TargetingContext(string id = null)
        {
            ID = id;
        }
    }
}
