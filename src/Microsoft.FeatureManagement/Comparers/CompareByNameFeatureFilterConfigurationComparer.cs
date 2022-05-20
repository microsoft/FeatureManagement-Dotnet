using System.Collections.Generic;

namespace Microsoft.FeatureManagement.Comparers
{
    /// <summary>
    /// Compares two FeatureFilterConfiguration using only the name
    /// </summary>
    public class CompareByNameFeatureFilterConfigurationComparer : IEqualityComparer<FeatureFilterConfiguration>
    {
        public bool Equals(FeatureFilterConfiguration x, FeatureFilterConfiguration y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Name == y.Name;
        }

        public int GetHashCode(FeatureFilterConfiguration obj)
        {
            return (obj.Name != null ? obj.Name.GetHashCode() : 0);
        }
    }
}