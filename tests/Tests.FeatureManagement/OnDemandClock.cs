using Microsoft.FeatureManagement.FeatureFilters;
using System;

namespace Tests.FeatureManagement
{
    internal class OnDemandClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
