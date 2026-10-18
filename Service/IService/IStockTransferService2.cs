using Common;
using Common.Stock2;

namespace Service.IService
{
    public interface IStockTransferService2
    {
        Task<BaseResponse<int>> CreateTransferAsync(TransferRequestDto2 request);
        Task<BaseResponse<bool>> ReceiveTransferAsync(int transferId, int? userId);
        Task<BaseResponse<bool>> CancelTransferAsync(int transferId, int? userId);
        Task<BaseResponse<PagedResult2<DocumentRowDto2>>> GetTransfersAsync(StockQueryFilter2 filter);
        Task<BaseResponse<List<DocumentLineDto2>>> GetTransferLinesAsync(int transferId);
    }
}
