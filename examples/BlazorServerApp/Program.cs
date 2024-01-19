using BlazorServerApp.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.FeatureManagement;

namespace BlazorServerApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddSingleton<WeatherForecastService>();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie();

            builder.Services.AddScoped<UserAgentContext>();

            //
            // To consume scoped services, AddScopedFeatureManagement should be used instead of AddFeatureManagement.
            // This will ensure that feature management services, including feature filters, targeting context accessor, are added as scoped services.
            builder.Services.AddScopedFeatureManagement()
                .WithTargeting<MyTargetingContextAccessor>()
                .AddFeatureFilter<BrowserFilter>();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}