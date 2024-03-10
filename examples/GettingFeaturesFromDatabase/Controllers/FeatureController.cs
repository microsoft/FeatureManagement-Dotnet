using GettingFeaturesFromDatabase.Database;
using GettingFeaturesFromDatabase.Database.Services;
using Microsoft.AspNetCore.Mvc;

namespace GettingFeaturesFromDatabase.Controllers;

[ApiController]
[Route("feature")]
public class FeatureController : ControllerBase
{
    private readonly IFeatureService _featureService;

    public FeatureController(IFeatureService featureService)
    {
        _featureService = featureService;
    }
    
    [HttpGet]
    public async Task<IReadOnlyCollection<Feature>> GetFeatures()
    {
        return await _featureService.GetFeatureAsync();
    }
    
    [HttpPut]
    public async Task UpdateFeature(string featureName, bool isEnabled)
    {
        await _featureService.UpdateFeatureAsync(featureName, isEnabled);
    }
}
