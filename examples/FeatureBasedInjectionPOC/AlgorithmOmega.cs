using Microsoft.FeatureManagement;

namespace FeatureBasedInjectionPOC
{
    [FeaturedServiceAlias("Omega")]
    internal class AlgorithmOmega : IAlgorithm
    {
        public string Name { get; set; }

        public AlgorithmOmega()
        {
            Name = "Omega";
        }
    }
}
