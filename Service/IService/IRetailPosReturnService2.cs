using Common;
using Common.RetailPos2;

namespace Service.IService
{
    public interface IRetailPosReturnService2
    {
        /// <summary>The invoice lines with how many pieces can still be returned.</summary>
        Task<BaseResponse<RetailPosReturnPrepDto2>> GetReturnPrepAsync(int saleId);

        /// <summary>
        /// Returns one or more products of an invoice. In one transaction: the return record, the credit on the client's
        /// ledger, a refund entry when money must be paid back, and the stock movement of every line.
        /// </summary>
        Task<BaseResponse<RetailPosReturnResultDto2>> ReturnItemsAsync(RetailPosReturnRequestDto2 request);

        Task<BaseResponse<List<RetailPosReturnRowDto2>>> GetReturnsAsync(RetailPosReturnFilterDto2 filter);
    }
}
