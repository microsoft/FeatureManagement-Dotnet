// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using System;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// Abstracts the system clock to facilitate testing.
    /// .NET8 offers an abstract class TimeProvider. After we stop supporting .NET version less than .NET8, this ISystemClock should retire.
    /// </summary>
    internal interface ISystemClock
    {
        /// <summary>
        /// Retrieves the current system time in UTC.
        /// </summary>
        public DateTimeOffset UtcNow { get; }
    }
}