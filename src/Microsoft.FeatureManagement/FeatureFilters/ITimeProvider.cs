// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using System;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// Provides the current time. This was implemented to allow the time window filter in our test suite to use simulated current time.
    /// </summary>
    internal interface ITimeProvider
    {
        /// <summary>
        /// Gets the current time.
        /// </summary>
        public DateTimeOffset GetTime();
    }
}