// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Telemetry.ApplicationInsights;
using VariantServiceDemo;

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
// Add variant implementations of ICalculator
builder.Services.AddSingleton<ICalculator, DefaultCalculator>();

builder.Services.AddSingleton<ICalculator, RemoteCalculator>();

//
// Enter feature management
//
// Enhance a web application with feature management
// Including user targeting capability and the variant service provider of ICalculator which is bounded with the variant feature flag "Calculator"
// Wire up evaluation event emission
builder.Services.AddFeatureManagement()
    .WithTargeting<HttpContextTargetingContextAccessor>()
    .WithVariantService<ICalculator>("Calculator")
    .AddApplicationInsightsTelemetryPublisher();

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
