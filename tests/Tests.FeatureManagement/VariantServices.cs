using Microsoft.FeatureManagement;

namespace Tests.FeatureManagement
{
    interface IAlgorithm
    {
        public string Style { get; }
    }

    class AlgorithmBeta : IAlgorithm
    {
        public string Style { get; set; }

        public AlgorithmBeta()
        {
            Style = "Beta";
        }
    }

    class AlgorithmSigma : IAlgorithm
    {
        public string Style { get; set; }

        public AlgorithmSigma()
        {
            Style = "Sigma";
        }
    }

    [VariantServiceAlias("Omega")]
    class AlgorithmOmega : IAlgorithm
    {
        public string Style { get; set; }

        public AlgorithmOmega(string style)
        {
            Style = style;
        }
    }

    [VariantServiceAlias("Big")]
    class BigAlgorithm : IAlgorithm
    {
        public string Style { get; } = "Big";
    }

    [VariantServiceAlias("Small")]
    class SmallAlgorithm : IAlgorithm
    {
        public string Style { get; } = "Small";
    }
}