using Domain.IRepositories;
using Microsoft.Extensions.DependencyInjection;

namespace Data.Repositories
{
    /// <summary>
    /// Creates a unit of work in its own DI scope. The Blazor Hybrid app keeps its main scope alive for the whole
    /// app session, so every stock operation gets a fresh DbContext instead of sharing that long-lived one.
    /// </summary>
    public class StockUnitOfWorkFactory2 : IStockUnitOfWorkFactory2
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public StockUnitOfWorkFactory2(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public IStockUnitOfWork2 Create()
        {
            return new StockUnitOfWork2(_scopeFactory.CreateScope());
        }
    }
}
