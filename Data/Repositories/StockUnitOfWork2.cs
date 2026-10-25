using Domain.IRepositories;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Data.Repositories
{
    /// <summary>One DI scope (so one DbContext) plus one optional transaction for a single stock operation.</summary>
    public class StockUnitOfWork2 : IStockUnitOfWork2
    {
        private readonly IServiceScope _scope;
        private readonly InventoryDbContext _db;
        private IDbContextTransaction? _transaction;

        public StockUnitOfWork2(IServiceScope scope)
        {
            _scope = scope;
            var sp = scope.ServiceProvider;
            _db = sp.GetRequiredService<InventoryDbContext>();
            Ledger = sp.GetRequiredService<IStockLedgerRepo2>();
            Master = sp.GetRequiredService<IStockMasterRepo2>();
            Receiving = sp.GetRequiredService<IStockReceivingRepo2>();
            Issues = sp.GetRequiredService<IStockIssueRepo2>();
            Transfers = sp.GetRequiredService<IStockTransferRepo2>();
            Adjustments = sp.GetRequiredService<IStockAdjustmentRepo2>();
            Reports = sp.GetRequiredService<IStockReportRepo2>();
        }

        public IStockLedgerRepo2 Ledger { get; }
        public IStockMasterRepo2 Master { get; }
        public IStockReceivingRepo2 Receiving { get; }
        public IStockIssueRepo2 Issues { get; }
        public IStockTransferRepo2 Transfers { get; }
        public IStockAdjustmentRepo2 Adjustments { get; }
        public IStockReportRepo2 Reports { get; }

        public async Task BeginTransactionAsync()
        {
            _transaction ??= await _db.Database.BeginTransactionAsync();
        }

        public Task SaveChangesAsync()
        {
            return StockDbSave2.SaveAsync(_db);
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
