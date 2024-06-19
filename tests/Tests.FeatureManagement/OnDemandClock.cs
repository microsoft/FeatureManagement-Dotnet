using System;
using Microsoft.FeatureManagement.FeatureFilters;

namespace Tests.FeatureManagement
{
    internal class OnDemandClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
