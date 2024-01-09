using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using FeatureBasedInjectionPOC;


IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

IServiceCollection services = new ServiceCollection();

services.AddSingleton(configuration)
        .AddFeatureManagement()
        .AddFeatureFilter<TargetingFilter>();

services.AddSingleton<IAlgorithm, AlgorithmAlpha>();

var targetingContextAccessor = new OnDemandTargetingContextAccessor();

services.AddSingleton<ITargetingContextAccessor>(targetingContextAccessor);

using (ServiceProvider serviceProvider = services.BuildServiceProvider())
{
    IVariantFeatureManager featureManager = serviceProvider.GetRequiredService<IVariantFeatureManager>();

    IFeaturedService<IAlgorithm> featuredAlgorithm = serviceProvider.GetRequiredService<IFeaturedService<IAlgorithm>>();

    targetingContextAccessor.Current = new TargetingContext
    {
        UserId = "Guest"
    };

    IAlgorithm algorithm = await featuredAlgorithm.GetAsync(CancellationToken.None);

    Console.WriteLine($"Get algorithm {algorithm.Name} because the feature flag is {await featureManager.IsEnabledAsync("MyFeature", CancellationToken.None)}");

    targetingContextAccessor.Current = new TargetingContext
    {
        UserId = "UserBeta"
    };

    algorithm = await featuredAlgorithm.GetAsync(CancellationToken.None);

    Console.WriteLine($"Get algorithm {algorithm.Name} because the feature flag is {await featureManager.IsEnabledAsync("MyFeature", CancellationToken.None)}");

    targetingContextAccessor.Current = new TargetingContext
    {
        UserId = "UserSigma"
    };

    algorithm = await featuredAlgorithm.GetAsync(CancellationToken.None);

    Variant variant = await featureManager.GetVariantAsync("MyFeature", CancellationToken.None);

    Console.WriteLine($"Get algorithm {algorithm.Name} because the feature variant is {variant.Name}");

    targetingContextAccessor.Current = new TargetingContext
    {
        UserId = "UserOmega"
    };

    algorithm = await featuredAlgorithm.GetAsync(CancellationToken.None);

    variant = await featureManager.GetVariantAsync("MyFeature", CancellationToken.None);

    Console.WriteLine($"Get algorithm {algorithm.Name} because the feature variant is {variant.Name}");
}
