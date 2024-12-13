// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

//
// Use cookie auth for simplicity and randomizing user
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/RandomizeUser";
    });

//
// What a web app using app insights looks like
//
// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddApplicationInsightsTelemetry();

//
// Enter feature management
//
// Enhance a web application with feature management
// Including user targeting capability
// Wire up evaluation event emission
builder.Services.AddFeatureManagement()
    .WithTargeting()
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

app.UseAuthentication();

app.UseAuthorization();

app.MapRazorPages();

//
// Add Targeting Id to HttpContext
app.UseMiddleware<TargetingHttpContextMiddleware>();

app.Run();
