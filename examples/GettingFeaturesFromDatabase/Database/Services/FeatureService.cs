using Microsoft.EntityFrameworkCore;

namespace GettingFeaturesFromDatabase.Database.Services;

public class FeatureService : IFeatureService
{
    private readonly SqliteDbContext _sqliteDbContext;
    
    public FeatureService(SqliteDbContext sqliteDbContext)
    {
        _sqliteDbContext = sqliteDbContext;
    }
    
    public async Task<Feature?> GetFeatureAsync(string featureName)
    {
        var feature = await _sqliteDbContext.Set<Feature>().FindAsync(featureName);

        return feature;
    }
    
    public async Task<IEnumerable<Feature>> GetFeatureAsync()
    {
        var features = await _sqliteDbContext.Set<Feature>().ToListAsync();

        return features;
    }
}
