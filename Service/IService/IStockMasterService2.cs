using Common;
using Common.Stock2;

namespace Service.IService
{
    /// <summary>Master data for the stock module: locations, suppliers (existing Vendor table) and reorder settings.</summary>
    public interface IStockMasterService2
    {
        Task<BaseResponse<List<StockLocationDto2>>> GetLocationsAsync(bool includeInactive);
        Task<BaseResponse<int>> SaveLocationAsync(StockLocationDto2 dto);

        Task<BaseResponse<List<StockSupplierDto2>>> GetSuppliersAsync(bool includeInactive);
        Task<BaseResponse<int>> SaveSupplierAsync(StockSupplierDto2 dto);

        Task<BaseResponse<PagedResult2<StockSettingDto2>>> GetSettingsAsync(StockQueryFilter2 filter);
        Task<BaseResponse<bool>> SaveSettingAsync(StockSettingDto2 dto, int? userId);
    }
}
