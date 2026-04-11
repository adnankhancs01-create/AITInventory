using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.IRepositories
{
    public interface ITransactionRepo
    {
        Task<VendorTransaction> AddEditTransactionAsync(VendorTransaction transaction);
        string GenerateSlip(VendorTransaction transaction,string? clientName);
        Task<TransactionSlip> AddEditTransactionSlipAsync(TransactionSlip transactionSlip);
        Task<(List<VendorTransaction>, int)> GetTransactionAsync(int? transactionId, int pageIndex, int pageSize, string? filter);
    }
}
