using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Telemetry.ApplicationInsights;
using Microsoft.FeatureManagement.Telemetry.ApplicationInsights.AspNetCore;
using VariantsDemo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddApplicationInsightsTelemetry();

// Add Azure Dynamic Config
builder.Configuration.AddAzureAppConfiguration(options =>
{
    var test = Environment.GetEnvironmentVariable("APP_CONFIG_CONNECTION_STRING");
    options.Connect(Environment.GetEnvironmentVariable("APP_CONFIG_CONNECTION_STRING"))
           .UseFeatureFlags();
});

//
// App Insights TargetingId Tagging
builder.Services.AddSingleton<ITelemetryInitializer, TargetingTelemetryInitializer>();

// Add FeatureManagement
builder.Services.AddFeatureManagement()
    .WithTargeting<HttpContextTargetingContextAccessor>()
    .AddTelemetryPublisher<ApplicationInsightsTelemetryPublisher>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();

//
// Adds Targeting Id to HttpContext
app.UseMiddleware<TargetingHttpContextMiddleware>();

var keys = app.Configuration.AsEnumerable().ToList();

app.Run();