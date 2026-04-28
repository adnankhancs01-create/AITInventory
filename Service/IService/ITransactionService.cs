using Common;
using Common.Models.RequestModel;
using Common.Models.ResponseModel;

namespace Service.IService
{
    public interface ITransactionService
    {
        Task PrintSlipAsync(string slipContent);
        Task<BaseResponse<int?>> ProcessTransactions(ProcessTransactionsModel transactionModel);
        Task<PagedTransactionResponse> GetTransactionsAsync(TransactionFilterRequest request);
        Task<ProcessTransactionsModel> GetTransactionByIdAsync(int transactionId);
        Task<BaseResponse<int?>> CreateReturnTransaction(ProcessTransactionsModel ProcessTransactionsModel);
        Task<BaseResponse<bool>> RevertTransaction(int transactionId);
        Task<BaseResponse<DashboardResponse>> GetDashboardAsync(DateTime? from, DateTime? to); 
    }
}
