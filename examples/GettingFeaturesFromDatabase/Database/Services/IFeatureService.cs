namespace GettingFeaturesFromDatabase.Database.Services;

public interface IFeatureService
{
    Task<Feature?> GetFeatureAsync(string featureName);
    
    Task<IReadOnlyCollection<Feature>> GetFeatureAsync();
    
    Task UpdateFeatureAsync(string featureName, bool isEnabled);
}
