using Microsoft.FeatureManagement.Telemetry.ApplicationInsights;
using EvaluationDataToApplicationInsights.Middlewares;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using EvaluationDataToApplicationInsights;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<ITargetingContextAccessor, HttpContextTargetingContextAccessor>();
builder.Services.AddFeatureManagement()
    .AddFeatureFilter<TargetingFilter>()
    .AddTelemetryPublisher<ApplicationInsightsTelemetryPublisher>();

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

//
// App Insights User Tagging
app.UseMetricsMiddleware();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
