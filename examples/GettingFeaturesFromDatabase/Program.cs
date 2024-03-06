using GettingFeaturesFromDatabase;
using GettingFeaturesFromDatabase.Database;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDatabase();
builder.Services.AddFeatureService();

/* Add feature management */
builder.Services.AddScoped<IFeatureDefinitionProvider, CustomFeatureDefinitionProvider>().AddScopedFeatureManagement();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();