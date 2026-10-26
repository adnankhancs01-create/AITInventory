using Domain.IRepositories;
using Microsoft.Extensions.DependencyInjection;

namespace Data.Repositories
{
    /// <summary>Creates a unit of work in its own DI scope, so every operation gets a fresh DbContext (see StockUnitOfWorkFactory2).</summary>
    public class UserUnitOfWorkFactory2 : IUserUnitOfWorkFactory2
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public UserUnitOfWorkFactory2(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public IUserUnitOfWork2 Create()
        {
            return new UserUnitOfWork2(_scopeFactory.CreateScope());
        }
    }
}
