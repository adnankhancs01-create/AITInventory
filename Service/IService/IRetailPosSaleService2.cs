using Common;
using Common.RetailPos2;
using Common.Stock2;

namespace Service.IService
{
    public interface IRetailPosSaleService2
    {
        Task<BaseResponse<List<RetailPosProductLookupDto2>>> GetProductLookupAsync();
        /// <summary>Active stock locations to sell from. The default location is created on first use.</summary>
        Task<BaseResponse<List<StockLocationDto2>>> GetLocationsAsync();
        /// <summary>Quantity available to sell per product id at one location (null = default). Products with no stock are missing from the result.</summary>
        Task<BaseResponse<Dictionary<int, int>>> GetAvailableStockAsync(int? locationId);
        /// <summary>Saves the invoice, its lines and the first payment (if any) in one transaction.</summary>
        Task<BaseResponse<RetailPosCheckoutResultDto2>> CheckoutAsync(RetailPosCheckoutRequestDto2 request);
        Task<BaseResponse<List<RetailPosSaleRowDto2>>> GetSalesAsync(RetailPosSaleFilterDto2 filter);
        Task<BaseResponse<RetailPosSaleDetailDto2>> GetSaleDetailAsync(int saleId);
    }
}
