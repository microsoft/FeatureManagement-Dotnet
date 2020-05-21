// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.AspNetCore.Authentication;

namespace FeatureFlagDemo.Authentication
{
	internal static class QueryStringAuthenticationExtensions
	{
		public static AuthenticationBuilder AddQueryString(this AuthenticationBuilder builder)
		{
			return builder.AddScheme<QueryStringAuthenticationOptions, QueryStringAuthenticationHandler>(
				Schemes.QueryString,
				null);
		}
	}
}
