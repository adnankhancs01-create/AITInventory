using Domain.Entities;
using Domain.Stock2;

namespace Domain.IRepositories
{
    /// <summary>
    /// Data access for the Stock ledger, stock locations and location balances. All methods work on the
    /// unit of work's own database context, so the caller controls the transaction.
    /// </summary>
    public interface IStockLedgerRepo2
    {
        /// <summary>Returns the default location, creating "MAIN" on first use (no seed data is needed in the migration).</summary>
        Task<StockLocation2> EnsureDefaultLocationAsync();

        /// <summary>Tracked location (can be modified and saved). Returns null when not found or when activeOnly is true and the location is inactive.</summary>
        Task<StockLocation2?> GetLocationAsync(int locationId, bool activeOnly);

        /// <summary>Active ledger rows (optionally for some products only).</summary>
        Task<List<StockLedgerRow2>> GetActiveLedgerAsync(ICollection<int>? productIds = null);

        /// <summary>On-hand quantity and FIFO layers per product.</summary>
        Task<Dictionary<int, StockProductState2>> GetStatesAsync(ICollection<int>? productIds = null);

        /// <summary>Sum of non-default location balances per product.</summary>
        Task<Dictionary<int, int>> GetBalanceTotalsAsync(ICollection<int>? productIds = null);

        /// <summary>Stored balances of one (non-default) location per product.</summary>
        Task<Dictionary<int, int>> GetLocationBalancesAsync(int locationId);

        /// <summary>Quantity currently travelling between locations (transfers with status InTransit).</summary>
        Task<Dictionary<int, int>> GetInTransitAsync(ICollection<int>? productIds = null);

        /// <summary>Quantity of one product at one location, read inside the caller's transaction.</summary>
        Task<int> GetQuantityAtLocationAsync(int productId, StockLocation2 location);

        /// <summary>Adds delta to the stored balance of a NON-default location. Throws when the result would be negative.</summary>
        Task ChangeBalanceAsync(int productId, StockLocation2 location, int delta);

        void AddLedgerRow(Stock row);
        void AddMovement(StockMovement2 movement);
        void AddBatch(StockBatch2 batch);

        /// <summary>Tracked ledger rows by id (used to deactivate them).</summary>
        Task<List<Stock>> GetLedgerRowsAsync(ICollection<int> ledgerIds);

        /// <summary>Names of active products keyed by id. Ids that are missing or inactive are not returned.</summary>
        Task<Dictionary<int, string>> GetActiveProductNamesAsync(ICollection<int> productIds);

        /// <summary>Names of products keyed by id, including inactive products.</summary>
        Task<Dictionary<int, string>> GetProductNamesAsync(ICollection<int> productIds);
    }
}
