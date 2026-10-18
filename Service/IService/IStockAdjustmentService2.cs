using Common;
using Common.Stock2;

namespace Service.IService
{
    public interface IStockAdjustmentService2
    {
        Task<BaseResponse<int>> PostAdjustmentAsync(AdjustmentRequestDto2 request);
        Task<BaseResponse<PagedResult2<DocumentRowDto2>>> GetAdjustmentsAsync(StockQueryFilter2 filter);
        Task<BaseResponse<List<DocumentLineDto2>>> GetAdjustmentLinesAsync(int adjustmentId);

        /// <summary>
        /// Count sheet for a location: one line per active product with the system quantity at that location
        /// (ProcessedQuantity) and CountedQuantity pre-filled with the same number (Quantity).
        /// </summary>
        Task<BaseResponse<List<DocumentLineDto2>>> GetCountSheetAsync(int locationId, int? categoryId, string? search);
    }
}
