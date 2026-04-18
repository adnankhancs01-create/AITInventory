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
        //Task<VendorTransaction> AddEditTransactionAsync(VendorTransaction transaction);
        Task<TransactionSlip> AddEditTransactionSlipAsync(TransactionSlip transactionSlip);
        Task<BaseResponse<int?>> ProcessTransactions(TransactionMst transactionMst);
        //Task<(List<VendorTransaction>, int)> GetTransactionAsync(int? transactionId, int pageIndex, int pageSize, string? filter);
        Task<PagedTransactionResponse> GetTransactionsAsync(TransactionFilterRequest request);
        Task<TransactionMst> GetTransactionByIdAsync(int transactionId);

    }
}
