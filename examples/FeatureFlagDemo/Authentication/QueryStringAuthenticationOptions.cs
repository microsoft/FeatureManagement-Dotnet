// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.AspNetCore.Authentication;

namespace FeatureFlagDemo.Authentication
{
	internal class QueryStringAuthenticationOptions : AuthenticationSchemeOptions
	{
		public string UsernameParameterName { get; } = "username";

		public string GroupsParameterName { get; } = "groups";
	}
}
