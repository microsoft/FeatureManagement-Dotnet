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

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddSingleton<WeatherForecastService>();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie();

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            builder.Services.AddSingleton(configuration)
                .AddScopedFeatureManagement() // Scoped Feature Management should be used in Blazor apps
                .WithTargeting<MyTargetingContextAccessor>()
                .AddFeatureFilter<MyAuthenticationFilter>();

            //
            // The recommended safe way to pass context to server-side Blazor app is to use scoped service, see https://learn.microsoft.com/en-us/aspnet/core/blazor/security/server/threat-mitigation?view=aspnetcore-7.0#ihttpcontextaccessorhttpcontext-in-razor-components.
            builder.Services.AddScoped<HttpContextProvider>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseCookiePolicy();
            app.UseAuthentication();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}