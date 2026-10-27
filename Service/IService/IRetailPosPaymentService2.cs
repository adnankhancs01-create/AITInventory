using Common;
using Common.RetailPos2;

namespace Service.IService
{
    public interface IRetailPosPaymentService2
    {
        /// <summary>Records a new payment (never edits an old one) against an invoice or the client's account balance.</summary>
        Task<BaseResponse<RetailPosReceiptDto2>> ReceivePaymentAsync(RetailPosReceivePaymentRequestDto2 request);
        Task<BaseResponse<RetailPosReceiptDto2>> GetReceiptAsync(int paymentId);
        Task<BaseResponse<List<RetailPosPaymentRowDto2>>> GetPaymentsAsync(RetailPosPaymentFilterDto2 filter);

        /// <summary>Owner only. Marks a payment as reversed with a reason; the original row is kept for the audit trail.</summary>
        Task<BaseResponse<bool>> ReversePaymentAsync(RetailPosReversePaymentRequestDto2 request);

        /// <summary>Owner only. Credit notes and approved balance adjustments, always with a reason.</summary>
        Task<BaseResponse<int>> AdjustBalanceAsync(RetailPosAdjustmentRequestDto2 request);
    }
}
