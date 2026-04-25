using Common;
using Common.Models.RequestModel;
using Common.Models.ResponseModel;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.IRepositories
{
    public interface ITransactionRepo
    {
        Task<TransactionSlip> AddEditTransactionSlipAsync(TransactionSlip transactionSlip);
        Task<BaseResponse<int?>> ProcessTransactions(TransactionMst transactionMst);
        Task<PagedTransactionResponse> GetTransactionsAsync(TransactionFilterRequest request);
        Task<TransactionMst> GetTransactionByIdAsync(int transactionId);
        Task<TransactionDetails?> GetTransactionDetailByProduct(int detailId, int productId);


        Task<BaseResponse<int?>> CreateReturnTransaction(
    int transactionId,
    int productId,
    int quantity,
    decimal amount,
    int stockQty,
    int createdBy);

        Task<BaseResponse<bool>> RevertTransaction(int id);
        Task<DashboardSummary> GetSummaryAsync(DateTime? from, DateTime? to);
        Task<List<TransactionSummaryData>> GetRecentTransactionsAsync(int count);
    }
}
