using Microsoft.FeatureManagement;

namespace FeatureBasedInjectionPOC
{
    [FeaturedServiceAlias("Omega")]
    class AlgorithmOmega : IAlgorithm
    {
        public string Name { get; set; }

        public AlgorithmOmega(string name)
        {
            Name = name;
        }
    }
}
