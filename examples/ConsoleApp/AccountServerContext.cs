using Consoto.Banking.AccountServer.FeatureFilters;
using Microsoft.FeatureManagement;

namespace Consoto.Banking.AccountServer
{
    class AccountServerContext : IAccountId, IFeatureFilterContext
    {
        public string AccountId { get; set; }
    }
}
