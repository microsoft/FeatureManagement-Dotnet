using Microsoft.FeatureManagement;

namespace Tests.FeatureManagement
{
    interface IAlgorithm
    {
        public string Style { get; }
    }

    class AlgorithmBeta : IAlgorithm
    {
        public static int Instances; // Tracks constructed instances
        public string Style { get; set; }

        public AlgorithmBeta()
        {
            Instances++;
            Style = "Beta";
        }
    }

    class AlgorithmSigma : IAlgorithm
    {
        public static int Instances; // Tracks constructed instances
        public string Style { get; set; }

        public AlgorithmSigma()
        {
            Instances++;
            Style = "Sigma";
        }
    }

    [VariantServiceAlias("Omega")]
    class AlgorithmOmega : IAlgorithm
    {
        public static int Instances; // Tracks constructed instances
        public string Style { get; set; }

        public AlgorithmOmega(string style)
        {
            Instances++;
            Style = style;
        }
    }
}
