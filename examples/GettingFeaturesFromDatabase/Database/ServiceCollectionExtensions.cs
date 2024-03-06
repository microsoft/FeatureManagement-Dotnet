using GettingFeaturesFromDatabase.Database.Services;
using Microsoft.EntityFrameworkCore;

namespace GettingFeaturesFromDatabase.Database;

public static class ServiceCollectionExtensions
{
    public static void AddDatabase(this IServiceCollection services)
    {
        services.AddDbContext<SqliteDbContext>(options =>
        {
            options.UseSqlite("Data Source=example.db");
        });
    }
    
    public static void AddFeatureService(this IServiceCollection services)
    {
        services.AddScoped<IFeatureService, FeatureService>();
    }
}
