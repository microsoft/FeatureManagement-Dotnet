using GettingFeaturesFromDatabase.Database;
using GettingFeaturesFromDatabase.Database.Services;
using Microsoft.FeatureManagement;

namespace GettingFeaturesFromDatabase;

public class CustomFeatureDefinitionProvider : IFeatureDefinitionProvider
{
    private readonly IFeatureService _featureService;
    
    public CustomFeatureDefinitionProvider(IFeatureService featureService)
    {
        _featureService = featureService;
    }
    
    public async Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
    {
        var feature = await _featureService.GetFeatureAsync(featureName);

        return GenerateFeatureDefinition(feature);
    }

    public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
    {
        var features = await _featureService.GetFeatureAsync();
        
        foreach (var feature in features)
        {
            yield return GenerateFeatureDefinition(feature);
        }
    }
    
    private FeatureDefinition GenerateFeatureDefinition(Feature? feature)
    {
        if (feature is null)
        {
            return new FeatureDefinition();
        }
        
        if (feature.IsEnabled)
        {
            return new FeatureDefinition
            {
                Name = feature.Name,
                EnabledFor = new[]
                {
                    new FeatureFilterConfiguration
                    {
                        Name = "AlwaysOn",
                    },
                },
            };
        }
        
        return new FeatureDefinition
        {
            Name = feature.Name,
        };
    }
}
