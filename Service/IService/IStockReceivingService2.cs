using Common;
using Common.Stock2;

namespace Service.IService
{
    /// <summary>Purchase orders and goods receipts.</summary>
    public interface IStockReceivingService2
    {
        Task<BaseResponse<int>> CreatePurchaseOrderAsync(PurchaseOrderRequestDto2 request);
        Task<BaseResponse<bool>> CancelPurchaseOrderAsync(int purchaseOrderId, int? userId);
        Task<BaseResponse<PagedResult2<DocumentRowDto2>>> GetPurchaseOrdersAsync(StockQueryFilter2 filter);
        Task<BaseResponse<List<DocumentLineDto2>>> GetPurchaseOrderLinesAsync(int purchaseOrderId);

        Task<BaseResponse<int>> PostReceiptAsync(ReceiptRequestDto2 request);
        Task<BaseResponse<bool>> CancelReceiptAsync(int receiptId, int? userId);
        Task<BaseResponse<PagedResult2<DocumentRowDto2>>> GetReceiptsAsync(StockQueryFilter2 filter);
        Task<BaseResponse<List<DocumentLineDto2>>> GetReceiptLinesAsync(int receiptId);
    }
}
