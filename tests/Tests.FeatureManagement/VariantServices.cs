using Microsoft.FeatureManagement;
using System.Collections.Generic;

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

    // Test service with tracking for lazy instantiation tests
    class AlgorithmAlpha : IAlgorithm
    {
        public string Style { get; set; }

        public AlgorithmAlpha(InstantiationTracker tracker)
        {
            Style = "Alpha";
            tracker.RecordInstantiation("Alpha");
        }
    }

    class AlgorithmGamma : IAlgorithm
    {
        public string Style { get; set; }

        public AlgorithmGamma(InstantiationTracker tracker)
        {
            Style = "Gamma";
            tracker.RecordInstantiation("Gamma");
        }
    }

    class AlgorithmDelta : IAlgorithm
    {
        public string Style { get; set; }

        public AlgorithmDelta(InstantiationTracker tracker)
        {
            Style = "Delta";
            tracker.RecordInstantiation("Delta");
        }
    }

    // Tracker to record which services are instantiated
    class InstantiationTracker
    {
        private readonly List<string> _instantiatedServices = new List<string>();

        public void RecordInstantiation(string serviceName)
        {
            _instantiatedServices.Add(serviceName);
        }

        public IReadOnlyList<string> InstantiatedServices => _instantiatedServices.AsReadOnly();
    }
}
