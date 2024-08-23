// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using EvaluationDataToApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Telemetry.ApplicationInsights;

var builder = WebApplication.CreateBuilder(args);

//
// What a web app using app insights looks like
//
// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();

builder.Services.AddApplicationInsightsTelemetry();

//
// App Insights TargetingId Tagging
builder.Services.AddSingleton<ITelemetryInitializer, TargetingTelemetryInitializer>();

//
// Enter feature management
//
// Enhance a web application with feature management
// Including user targeting capability
// Wire up evaluation event emission
builder.Services.AddFeatureManagement()
    .WithTargeting<HttpContextTargetingContextAccessor>()
    .AddApplicationInsightsTelemetry();

//
// Default code from .NET template below
//
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
// Add Targeting Id to HttpContext
app.UseMiddleware<TargetingHttpContextMiddleware>();

app.Run();
