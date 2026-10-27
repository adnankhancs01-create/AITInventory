using Domain.IRepositories;
using Microsoft.Extensions.DependencyInjection;

namespace Data.Repositories
{
    /// <summary>Creates a unit of work in its own DI scope so every operation gets a fresh DbContext (same idea as StockUnitOfWorkFactory2).</summary>
    public class RetailPosUnitOfWorkFactory2 : IRetailPosUnitOfWorkFactory2
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public RetailPosUnitOfWorkFactory2(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public IRetailPosUnitOfWork2 Create()
        {
            return new RetailPosUnitOfWork2(_scopeFactory.CreateScope());
        }
    }
}
