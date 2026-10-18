using Common;
using Common.Stock2;

namespace Service.IService
{
    /// <summary>Read-only dashboards, lists and reports.</summary>
    public interface IStockQueryService2
    {
        Task<BaseResponse<StockDashboardDto2>> GetDashboardAsync();
        Task<BaseResponse<PagedResult2<StockOverviewRowDto2>>> GetOverviewAsync(StockQueryFilter2 filter);
        Task<BaseResponse<PagedResult2<StockMovementRowDto2>>> GetMovementsAsync(StockQueryFilter2 filter);
        Task<BaseResponse<PagedResult2<StockValuationRowDto2>>> GetValuationAsync(StockQueryFilter2 filter);
        Task<BaseResponse<PagedResult2<StockAgingRowDto2>>> GetAgingAsync(StockQueryFilter2 filter);
        Task<BaseResponse<PagedResult2<StockExpiryRowDto2>>> GetExpiryAsync(StockQueryFilter2 filter);
        Task<BaseResponse<PagedResult2<StockReconciliationRowDto2>>> GetReconciliationAsync(StockQueryFilter2 filter);
        Task<BaseResponse<List<StockReorderSuggestionDto2>>> GetReorderSuggestionsAsync();
        Task<BaseResponse<List<ProductLookupDto2>>> GetProductLookupAsync();
    }
}
