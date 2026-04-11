using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories
{
    public class TransactionRepo : ITransactionRepo
    {
        private readonly InventoryDbContext _dbContext;
        public TransactionRepo(InventoryDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<VendorTransaction> AddEditTransactionAsync(VendorTransaction transaction)
        {
            if (transaction.Id > 0)
                _dbContext.VendorTransaction.Update(transaction);
            else
                await _dbContext.VendorTransaction.AddAsync(transaction);

            await _dbContext.SaveChangesAsync();

            return transaction;
        }
        public string GenerateSlip(VendorTransaction transaction, string? clientName)
        {
            var sb = new StringBuilder();
            clientName= string.IsNullOrEmpty(clientName) ? $"{transaction?.Client?.FirstName} {transaction?.Client?.LastName}:" : clientName;
            sb.AppendLine("========== Transaction Slip ==========");
            sb.AppendLine($"Slip No: {transaction.TransactionNumber}");
            sb.AppendLine($"Date: {transaction.TransactionDate?.ToString("dd-MMM-yyyy HH:mm")}");
            sb.AppendLine("--------------------------------------");
            sb.AppendLine($"Client: {clientName}");
            sb.AppendLine($"Product: {transaction?.Product?.Name}");
            sb.AppendLine($"Type: {transaction.TransactionType}");
            sb.AppendLine($"Quantity: {transaction.Quantity}");
            sb.AppendLine($"Unit Price: {transaction.UnitPrice:C}");
            sb.AppendLine($"Total Amount: {transaction.TotalAmount:C}");
            sb.AppendLine($"Client Amount: {transaction.ClientAmount:C}");//recieved amount
            sb.AppendLine("--------------------------------------");
            sb.AppendLine($"Remarks: {transaction.Remarks}");
            sb.AppendLine("======================================");

            return sb.ToString();
        }
        public async Task<TransactionSlip> AddEditTransactionSlipAsync(TransactionSlip transactionSlip)
        {
            if (transactionSlip.Id > 0)
                _dbContext.TransactionSlip.Update(transactionSlip);
            else
                await _dbContext.TransactionSlip.AddAsync(transactionSlip);

            await _dbContext.SaveChangesAsync();

            return transactionSlip;
        }

        public async Task<(List<VendorTransaction>, int)> GetTransactionAsync(int? transactionId, int pageIndex, int pageSize, string? filter)
        {
            var query = _dbContext.VendorTransaction
                .Include(t => t.Client)
                .Include(t => t.Product)
                .AsQueryable();
            if (transactionId.HasValue && transactionId>0)
                query = query.Where(t => t.Id == transactionId.Value);
            if (!string.IsNullOrEmpty(filter))
                query = query.Where(t => t.Client.FirstName.Contains(filter) || t.Client.LastName.Contains(filter) || t.Product.Name.Contains(filter));
            var totalRecords = await query.CountAsync();
            var transactions = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return (transactions, totalRecords);
        }

    }
}
