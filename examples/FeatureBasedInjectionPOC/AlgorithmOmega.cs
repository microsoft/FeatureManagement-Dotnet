using Microsoft.FeatureManagement;

namespace FeatureBasedInjectionPOC
{
    [VariantServiceAlias("Omega")]
    class AlgorithmOmega : IAlgorithm
    {
        public string Name { get; set; }

        public AlgorithmOmega(string name)
        {
            Name = name;
        }
    }
}
