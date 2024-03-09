namespace GettingFeaturesFromDatabase.Database.Services;

public interface IFeatureService
{
    Task<Feature?> GetFeatureAsync(string featureName);
    
    Task<IEnumerable<Feature>> GetFeatureAsync();
}
