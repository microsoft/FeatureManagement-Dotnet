using Microsoft.FeatureManagement.FeatureFilters;
using System;

namespace Tests.FeatureManagement
{
    internal class OnDemandTimeProvider : ITimeProvider
    {
        public DateTimeOffset Now { get; set; }

        public DateTimeOffset GetTime()
        {
            return Now;
        }
    }
}
