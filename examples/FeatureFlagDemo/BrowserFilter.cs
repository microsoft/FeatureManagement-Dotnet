// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;

namespace FeatureFlagDemo
{
	[FilterAlias("Browser")]
	public class BrowserFilter : IFeatureFilter
	{
		private const string Chrome = "Chrome";
		private const string Edge = "Edge";

		private readonly IHttpContextAccessor _httpContextAccessor;

		public BrowserFilter(IHttpContextAccessor httpContextAccessor)
		{
			_httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
		}

		public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
		{
			var settings = context.Parameters.Get<BrowserFilterSettings>() ?? new BrowserFilterSettings();

			if (settings.AllowedBrowsers.Any(browser => browser.Equals(Chrome, StringComparison.OrdinalIgnoreCase))
			    && IsChrome())
			{
				return Task.FromResult(true);
			}

			return Task.FromResult(settings.AllowedBrowsers
				                       .Any(browser => browser.Equals(Edge, StringComparison.OrdinalIgnoreCase))
			                       && IsEdge());
		}

		private bool IsChrome()
		{
			string userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"];

			return userAgent != null && userAgent.Contains(Chrome, StringComparison.OrdinalIgnoreCase) &&
			       !userAgent.Contains(Edge, StringComparison.OrdinalIgnoreCase);
		}

		private bool IsEdge()
		{
			// Return true if current request is sent from Edge browser

			string userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"];

			return userAgent != null && userAgent.Contains(Edge, StringComparison.OrdinalIgnoreCase) &&
			       !userAgent.Contains(Chrome, StringComparison.OrdinalIgnoreCase);
		}
	}
}
