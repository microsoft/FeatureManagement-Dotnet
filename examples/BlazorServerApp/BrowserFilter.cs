using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;

namespace BlazorServerApp
{
    [FilterAlias("Browser")]
    public class BrowserFilter : IFeatureFilter
    {
        private readonly ITargetingContextAccessor _targetingContextAccessor;

        public BrowserFilter(ITargetingContextAccessor targetingContextAccessor)
        {
            _targetingContextAccessor = targetingContextAccessor ?? throw new ArgumentNullException(nameof(targetingContextAccessor));
        }

        public async Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            BrowserFilterSettings settings = context.Parameters.Get<BrowserFilterSettings>() ?? new BrowserFilterSettings();

            foreach (string browser in settings.AllowedBrowsers)
            {
                TargetingContext targetingContext = await _targetingContextAccessor.GetContextAsync();

                if (targetingContext.Groups
                    .Any(group =>
                        string.Equals(group, browser, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
