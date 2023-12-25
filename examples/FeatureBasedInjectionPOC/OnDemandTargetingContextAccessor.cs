using Microsoft.FeatureManagement.FeatureFilters;

namespace FeatureBasedInjectionPOC
{
    class OnDemandTargetingContextAccessor : ITargetingContextAccessor
    {
        public TargetingContext Current { get; set; }

        public ValueTask<TargetingContext> GetContextAsync()
        {
            return new ValueTask<TargetingContext>(Current);
        }
    }
}
