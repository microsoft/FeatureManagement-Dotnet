using Microsoft.FeatureManagement.FeatureFilters;
using System;

namespace Tests.FeatureManagement
{
    class OnDemandClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
