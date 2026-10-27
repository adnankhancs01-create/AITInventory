using Common;
using Common.RetailPos2;

namespace Service.IService
{
    public interface IRetailPosSaleService2
    {
        Task<BaseResponse<List<RetailPosProductLookupDto2>>> GetProductLookupAsync();
        /// <summary>Saves the invoice, its lines and the first payment (if any) in one transaction.</summary>
        Task<BaseResponse<RetailPosCheckoutResultDto2>> CheckoutAsync(RetailPosCheckoutRequestDto2 request);
        Task<BaseResponse<List<RetailPosSaleRowDto2>>> GetSalesAsync(RetailPosSaleFilterDto2 filter);
        Task<BaseResponse<RetailPosSaleDetailDto2>> GetSaleDetailAsync(int saleId);
    }
}
