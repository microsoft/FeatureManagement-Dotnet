using System;

namespace Tests.FeatureManagement
{
    class OnDemandClock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; }

        public override DateTimeOffset GetUtcNow()
        {
            return UtcNow;
        }
    }
}
