using Microsoft.FeatureManagement;

namespace Consoto.Banking.AccountServer.FeatureFilters
{
    public interface IAccountId : IFeatureFilterContext
    {
        string AccountId { get; }
    }
}
