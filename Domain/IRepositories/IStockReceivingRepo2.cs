using Common.Stock2;
using Domain.Entities;

namespace Domain.IRepositories
{
    /// <summary>Data access for purchase orders and goods receipts.</summary>
    public interface IStockReceivingRepo2
    {
        // ---------------------------------------------------------------- purchase orders
        Task<int> CountPurchaseOrdersWithPrefixAsync(string numberPrefix);
        void AddPurchaseOrder(PurchaseOrder2 purchaseOrder);

        /// <summary>Tracked purchase order including its lines. Null when not found.</summary>
        Task<PurchaseOrder2?> GetPurchaseOrderWithLinesAsync(int purchaseOrderId);

        /// <summary>
        /// Marks the purchase order as modified so that its RowVersion is checked on save
        /// (blocks two users receiving the same order at the same time).
        /// </summary>
        void MarkPurchaseOrderModified(PurchaseOrder2 purchaseOrder);

        Task<List<DocumentRowDto2>> GetPurchaseOrderRowsAsync(StockQueryFilter2 filter);
        Task<List<DocumentLineDto2>> GetPurchaseOrderLinesAsync(int purchaseOrderId);

        /// <summary>Quantity still to be received on open / partially received orders, per product.</summary>
        Task<Dictionary<int, int>> GetOnOrderQuantitiesAsync(ICollection<int> productIds);
        Task<int> CountOpenPurchaseOrdersAsync();

        // ---------------------------------------------------------------- receipts
        Task<int> CountReceiptsWithPrefixAsync(string numberPrefix);
        void AddReceipt(StockReceipt2 receipt);

        /// <summary>Tracked receipt including its lines (with their purchase order line) and its location. Null when not found.</summary>
        Task<StockReceipt2?> GetReceiptForCancelAsync(int receiptId);

        Task<List<DocumentRowDto2>> GetReceiptRowsAsync(StockQueryFilter2 filter);
        Task<List<DocumentLineDto2>> GetReceiptLinesAsync(int receiptId);
    }
}
