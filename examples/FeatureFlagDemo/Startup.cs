// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using FeatureFlagDemo.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;

namespace FeatureFlagDemo
{
	public class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			services.Configure<CookiePolicyOptions>(options =>
			{
				options.CheckConsentNeeded = context => true;
				options.MinimumSameSitePolicy = SameSiteMode.None;
			});

			services.AddAuthentication(Schemes.QueryString)
				.AddQueryString();

			//
			// Enable the use of IHttpContextAccessor
			services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

			//
			// Add required services for TargetingFilter
			services.AddSingleton<ITargetingContextAccessor, HttpContextTargetingContextAccessor>();

			services.AddFeatureManagement()
				.AddFeatureFilter<BrowserFilter>()
				.AddFeatureFilter<TimeWindowFilter>()
				.AddFeatureFilter<PercentageFilter>()
				.AddFeatureFilter<TargetingFilter>()
				.UseDisabledFeaturesHandler(disabledFeaturesHandler: new FeatureNotEnabledDisabledHandler());

			services.AddControllers();

			services.AddMvc(options =>
			{
				options.Filters.AddForFeature<ThirdPartyActionFilter>(nameof(MyFeatureFlags.EnhancedPipeline));
			}).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
				app.UseHsts();
			}

			app.UseAzureAppConfiguration();

			app.UseAuthentication();

			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseRouting();

			app.UseMiddlewareForFeature<ThirdPartyMiddleware>(nameof(MyFeatureFlags.EnhancedPipeline));

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller}/{action}/{id?}",
					defaults: new {controller = "Home", action = "Index"});
			});
		}
	}
}
