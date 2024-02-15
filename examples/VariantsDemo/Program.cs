using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

// Add Razor Pages
builder.Services.AddRazorPages();

// Add Azure Dynamic Config
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(Environment.GetEnvironmentVariable("APP_CONFIG_CONNECTION_STRING"))
           .UseFeatureFlags();
});

// Add FeatureManagement
builder.Services.AddFeatureManagement();

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

app.Run();
