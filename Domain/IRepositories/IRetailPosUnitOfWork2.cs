namespace Domain.IRepositories
{
    /// <summary>
    /// One database context plus one transaction for one Retail POS operation. A sale, its lines and its first payment
    /// are saved together or not at all. Dispose it when finished: an uncommitted transaction is rolled back.
    /// </summary>
    public interface IRetailPosUnitOfWork2 : IAsyncDisposable
    {
        IRetailPosClientRepo2 Clients { get; }
        IRetailPosSaleRepo2 Sales { get; }
        IRetailPosPaymentRepo2 Payments { get; }
        IRetailPosReturnRepo2 Returns { get; }

        /// <summary>Stock ledger and locations. Shares the same database context and transaction, so a sale and its stock-out are saved together.</summary>
        IStockLedgerRepo2 Stock { get; }
        IStockMasterRepo2 StockMaster { get; }

        Task BeginTransactionAsync();

        /// <summary>Saves pending changes. Throws RetailPosConcurrencyException2 when a client or invoice was changed by someone else.</summary>
        Task SaveChangesAsync();
        Task CommitAsync();
    }
}
