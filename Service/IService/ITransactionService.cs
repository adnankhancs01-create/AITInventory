using Common;
using Common.Models;
using Common.Models.RequestModel;
using Common.Models.ResponseModel;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.IService
{
    public interface ITransactionService
    {
        //Task<BaseResponse<AddEditTransactionResponseModel>> AddEditTransactionAsync(AddEditTransactionRequestModel request);
        Task PrintSlipAsync(string slipContent);
        //Task<BaseResponse<(List<GetTransactionResponseModel>, int)>> GetTransactionAsync(int? transactionId, int pageIndex, int pageSize, string? Filter);
        Task<BaseResponse<int?>> ProcessTransactions(ProcessTransactionsModel transactionModel);
        Task<PagedTransactionResponse> GetTransactionsAsync(TransactionFilterRequest request);
        Task<ProcessTransactionsModel> GetTransactionByIdAsync(int transactionId);
    }
}
