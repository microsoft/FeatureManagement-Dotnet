// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.FeatureManagement;
using OpenTelemetry;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Use cookie auth for simplicity and randomizing user
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/RandomizeUser";
    });

//
// What a web app using OpenTelemetry looks like
//
// Add services to the container.
builder.Services.AddRazorPages();

//
// UseAzureMonitor() (from Azure.Monitor.OpenTelemetry.AspNetCore) wires up tracing, logging, and
// metrics along with the Azure Monitor exporter for all three in a single call. 
// It requires a valid connection string, so it's only added when one is configured 
// (via "APPLICATIONINSIGHTS_CONNECTION_STRING" in appsettings.json below,
// or as an environment variable of the same name); otherwise it would throw at startup. 
string connectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

OpenTelemetryBuilder openTelemetryBuilder = builder.Services.AddOpenTelemetry();

if (!string.IsNullOrEmpty(connectionString))
{
    openTelemetryBuilder.UseAzureMonitor(o => o.ConnectionString = connectionString);
}

openTelemetryBuilder
    .WithMetrics(metrics => metrics.AddMeter("OpenTelemetryDemo"));

//
// Enter feature management
//
// Enhance a web application with feature management
// Including user targeting capability
// Wire up OpenTelemetry evaluation event emission
builder.Services.AddFeatureManagement()
    .WithTargeting()
    .AddOpenTelemetry();

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
