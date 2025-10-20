// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using VariantServiceDemo;

var builder = WebApplication.CreateBuilder(args);

//
// What a web app using app insights looks like
//
// Add services to the container.
builder.Services.AddRazorPages();

//
// Use cookie auth for simplicity and randomizing user
builder.Services.AddAuthentication("CookieAuth");

builder.Services.AddApplicationInsightsTelemetry();

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
    .WithTargeting()
    .WithVariantService<ICalculator>("Calculator")
    .AddApplicationInsightsTelemetry();

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
