using Microsoft.FeatureManagement;

namespace BlazorServerApp
{
    [FilterAlias("Browser")]
    public class BrowserFilter : IFeatureFilter
    {
        private const string Chrome = "Chrome";
        private const string Edge = "Edge";
        private const string Firefox = "Firefox";

        private readonly UserAgentContext _userAgentContextProvider;

        public BrowserFilter(UserAgentContext userAgentContextProvider)
        {
            _userAgentContextProvider = userAgentContextProvider ?? throw new ArgumentNullException(nameof(userAgentContextProvider));
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            BrowserFilterSettings settings = context.Parameters.Get<BrowserFilterSettings>() ?? new BrowserFilterSettings();

            string userAgentContext = _userAgentContextProvider.UserAgent;

            if (settings.AllowedBrowsers.Any(browser => browser.Equals(Chrome, StringComparison.OrdinalIgnoreCase)) && IsChromeBrowser(userAgentContext))
            {
                return Task.FromResult(true);
            }
            else if (settings.AllowedBrowsers.Any(browser => browser.Equals(Edge, StringComparison.OrdinalIgnoreCase)) && IsEdgeBrowser(userAgentContext))
            {
                return Task.FromResult(true);
            }
            else if (settings.AllowedBrowsers.Any(browser => browser.Equals(Firefox, StringComparison.OrdinalIgnoreCase)) && IsFirefoxBrowser(userAgentContext))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        private static bool IsChromeBrowser(string userAgentContext)
        {
            if (userAgentContext == null)
            {
                return false;
            }

            return userAgentContext.Contains("chrome", StringComparison.OrdinalIgnoreCase) &&
                !userAgentContext.Contains("edg", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEdgeBrowser(string userAgentContext)
        {
            if (userAgentContext == null)
            {
                return false;
            }

            return userAgentContext.Contains("edg", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFirefoxBrowser(string userAgentContext)
        {
            if (userAgentContext == null)
            {
                return false;
            }

            return userAgentContext.Contains("firefox", StringComparison.OrdinalIgnoreCase);
        }
    }
}
