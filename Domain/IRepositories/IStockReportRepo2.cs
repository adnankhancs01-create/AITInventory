using Common.Stock2;
using Domain.Entities;
using Domain.Stock2;

namespace Domain.IRepositories
{
    /// <summary>
    /// Read-only data access for the overview, reports and dashboard. It returns raw data;
    /// the rules (status, aging buckets, reconciliation issues...) are applied by the service layer.
    /// </summary>
    public interface IStockReportRepo2
    {
        /// <summary>Products (with category name) matching the product-related filters (ProductId, CategoryId, Search).</summary>
        Task<List<StockProductInfo2>> GetProductsAsync(StockQueryFilter2 filter, bool includeInactive);

        /// <summary>Latest selling price per product code (from the Pricing table).</summary>
        Task<Dictionary<string, decimal?>> GetLatestSellPricesAsync();

        /// <summary>Date of the latest active ledger row per product.</summary>
        Task<Dictionary<int, DateTime?>> GetLastMovementDatesAsync();

        Task<StockLocation2?> FindLocationAsync(int locationId);
        Task<Dictionary<int, string>> GetLocationNamesAsync();

        /// <summary>Ledger rows with their sale/purchase transaction info, optionally limited to products and a date range.</summary>
        Task<List<StockLedgerMovementRow2>> GetLedgerMovementRowsAsync(ICollection<int>? productIds, DateTime? fromDate, DateTime? toExclusive);

        /// <summary>Stock2 movement records that belong to the given ledger rows.</summary>
        Task<List<StockMovement2>> GetMovementsForLedgerRowsAsync(ICollection<int> ledgerIds);

        /// <summary>Stock2 movement records without a ledger row (transfer dispatch / receive / cancel).</summary>
        Task<List<StockMovement2>> GetLocationOnlyMovementsAsync(ICollection<int>? productIds, DateTime? fromDate, DateTime? toExclusive, int? locationId, string? movementType);

        /// <summary>Batches that have an expiry date, optionally limited to products.</summary>
        Task<List<StockBatchInfo2>> GetBatchesWithExpiryAsync(ICollection<int>? productIds);
    }
}
