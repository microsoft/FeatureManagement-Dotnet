using GettingFeaturesFromDatabase;
using GettingFeaturesFromDatabase.Database;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDatabase();
builder.Services.AddFeatureService();

/* Add feature management */
builder.Services.AddScoped<IFeatureDefinitionProvider, CustomFeatureDefinitionProvider>().AddScopedFeatureManagement();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
