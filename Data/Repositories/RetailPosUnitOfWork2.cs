using Domain.IRepositories;
using Domain.RetailPos2;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Data.Repositories
{
    /// <summary>One DI scope (so one DbContext) plus one optional transaction for a Retail POS operation.</summary>
    public class RetailPosUnitOfWork2 : IRetailPosUnitOfWork2
    {
        private readonly IServiceScope _scope;
        private readonly InventoryDbContext _db;
        private IDbContextTransaction? _transaction;

        public RetailPosUnitOfWork2(IServiceScope scope)
        {
            _scope = scope;
            var sp = scope.ServiceProvider;
            _db = sp.GetRequiredService<InventoryDbContext>();
            Clients = sp.GetRequiredService<IRetailPosClientRepo2>();
            Sales = sp.GetRequiredService<IRetailPosSaleRepo2>();
            Payments = sp.GetRequiredService<IRetailPosPaymentRepo2>();
            Returns = sp.GetRequiredService<IRetailPosReturnRepo2>();
            Stock = sp.GetRequiredService<IStockLedgerRepo2>();
            StockMaster = sp.GetRequiredService<IStockMasterRepo2>();
        }

        public IRetailPosClientRepo2 Clients { get; }
        public IRetailPosSaleRepo2 Sales { get; }
        public IRetailPosPaymentRepo2 Payments { get; }
        public IRetailPosReturnRepo2 Returns { get; }
        public IStockLedgerRepo2 Stock { get; }
        public IStockMasterRepo2 StockMaster { get; }

        public async Task BeginTransactionAsync()
        {
            _transaction ??= await _db.Database.BeginTransactionAsync();
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new RetailPosConcurrencyException2(ex);
            }
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
                // Not committed: disposing rolls the transaction back, so no half-finished financial record remains.
                await _transaction.DisposeAsync();
                _transaction = null;
            }

            if (_scope is IAsyncDisposable asyncScope) await asyncScope.DisposeAsync();
            else _scope.Dispose();
        }
    }
}
