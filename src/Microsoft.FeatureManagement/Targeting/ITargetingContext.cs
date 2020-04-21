// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// Contextual information that is required to perform a targeting evaluation.
    /// </summary>
    public interface ITargetingContext
    {
        /// <summary>
        /// The user id that should be considered when evaluating if the context is being targeted.
        /// </summary>
        string UserId { get; set; }

        /// <summary>
        /// The groups that should be considered when evaluating if the context is being targeted.
        /// </summary>
        IEnumerable<string> Groups { get; set; }
    }
}
