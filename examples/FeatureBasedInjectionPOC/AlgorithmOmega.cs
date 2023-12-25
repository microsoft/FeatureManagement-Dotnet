namespace FeatureBasedInjectionPOC
{
    internal class AlgorithmOmega : IAlgorithm
    {
        public string Name { get; set; }

        public AlgorithmOmega(string name)
        {
            Name = name;
        }
    }
}
