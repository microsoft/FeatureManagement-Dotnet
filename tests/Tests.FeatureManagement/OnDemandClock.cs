using System;
using Microsoft.FeatureManagement.FeatureFilters;

namespace Tests.FeatureManagement
{
    class OnDemandClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
