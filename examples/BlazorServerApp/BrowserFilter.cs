using Microsoft.FeatureManagement;

namespace BlazorServerApp
{
    [FilterAlias("Browser")]
    public class BrowserFilter : IFeatureFilter
    {
        private readonly UserAgentContextProvider _userAgentContextProvider;

        public BrowserFilter(UserAgentContextProvider userAgentContextProvider)
        {
            _userAgentContextProvider = userAgentContextProvider ?? throw new ArgumentNullException(nameof(userAgentContextProvider));
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            string userAgentContext = _userAgentContextProvider.Context;

            return Task.FromResult(IsEdgeBrowser(userAgentContext));
        }

        private static bool IsEdgeBrowser(string userAgentContext)
        {
            if (userAgentContext == null)
            {
                return false;
            }

            return userAgentContext.Contains("edge", StringComparison.OrdinalIgnoreCase) || userAgentContext.Contains("edg", StringComparison.OrdinalIgnoreCase);
        }
    }
}
