using Domain.IRepositories;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Data.Repositories
{
    /// <summary>One DI scope (so one DbContext) plus one optional transaction for a user-management operation.</summary>
    public class UserUnitOfWork2 : IUserUnitOfWork2
    {
        private readonly IServiceScope _scope;
        private readonly InventoryDbContext _db;
        private IDbContextTransaction? _transaction;

        public UserUnitOfWork2(IServiceScope scope)
        {
            _scope = scope;
            var sp = scope.ServiceProvider;
            _db = sp.GetRequiredService<InventoryDbContext>();
            Accounts = sp.GetRequiredService<IUserAccountRepo2>();
            Roles = sp.GetRequiredService<IUserRoleRepo2>();
        }

        public IUserAccountRepo2 Accounts { get; }
        public IUserRoleRepo2 Roles { get; }

        public async Task BeginTransactionAsync()
        {
            _transaction ??= await _db.Database.BeginTransactionAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task CommitAsync()
        {
            if (_transaction == null) return;
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
            {
                // Not committed: disposing rolls the transaction back.
                await _transaction.DisposeAsync();
                _transaction = null;
            }

            if (_scope is IAsyncDisposable asyncScope) await asyncScope.DisposeAsync();
            else _scope.Dispose();
        }
    }
}
