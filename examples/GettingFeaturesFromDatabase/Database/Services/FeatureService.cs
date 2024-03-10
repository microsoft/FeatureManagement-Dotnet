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
    
    public async Task<IReadOnlyCollection<Feature>> GetFeatureAsync()
    {
        var features = await _sqliteDbContext.Set<Feature>().ToListAsync();

        return features;
    }
    
    public async Task UpdateFeatureAsync(string featureName, bool isEnabled)
    {
        var feature = await _sqliteDbContext.Set<Feature>().FindAsync(featureName);
        if (feature != null) feature.IsEnabled = isEnabled;

        await _sqliteDbContext.SaveChangesAsync();
    }
}
