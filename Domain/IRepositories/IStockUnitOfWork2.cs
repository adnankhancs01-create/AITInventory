namespace Domain.IRepositories
{
    /// <summary>
    /// One database context plus one transaction for one stock operation. All repositories of the unit of work
    /// share the same context, so changes made through different repositories are saved together.
    /// Dispose it when the operation is finished: an uncommitted transaction is rolled back.
    /// </summary>
    public interface IStockUnitOfWork2 : IAsyncDisposable
    {
        IStockLedgerRepo2 Ledger { get; }
        IStockMasterRepo2 Master { get; }
        IStockReceivingRepo2 Receiving { get; }
        IStockIssueRepo2 Issues { get; }
        IStockTransferRepo2 Transfers { get; }
        IStockAdjustmentRepo2 Adjustments { get; }
        IStockReportRepo2 Reports { get; }

        /// <summary>Starts a database transaction. Call CommitAsync to keep the changes.</summary>
        Task BeginTransactionAsync();

        /// <summary>Saves all pending changes. Throws StockConcurrencyException2 when a row was changed by someone else.</summary>
        Task SaveChangesAsync();

        Task CommitAsync();
    }
}
