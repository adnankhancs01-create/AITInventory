using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories
{
    public class TransactionRepo
    {
        private readonly InventoryDbContext _dbContext;
        public TransactionRepo(InventoryDbContext dbContext)
        {
            _dbContext=dbContext;
        }
        public async Task<VendorTransaction> AddTransactionAsync(VendorTransaction transaction)
        {
            await _dbContext.VendorTransaction.AddAsync(transaction);
            await _dbContext.SaveChangesAsync();

            return transaction;
        }
        public string GenerateSlip(VendorTransaction transaction)
        {
            var sb = new StringBuilder();

            sb.AppendLine("========== Transaction Slip ==========");
            sb.AppendLine($"Slip No: {transaction.Id}");
            sb.AppendLine($"Date: {transaction.TransactionDate?.ToString("dd-MMM-yyyy HH:mm")}");
            sb.AppendLine("--------------------------------------");
            sb.AppendLine($"Client: {transaction.Client.ClientDetail.FirstName} {transaction.Client.ClientDetail.LastName}");
            sb.AppendLine($"Product: {transaction.Product?.Name}");
            sb.AppendLine($"Type: {transaction.TransactionType}");
            sb.AppendLine($"Quantity: {transaction.Quantity}");
            sb.AppendLine($"Unit Price: {transaction.UnitPrice:C}");
            sb.AppendLine($"Total Amount: {transaction.TotalAmount:C}");
            sb.AppendLine($"Client Amount: {transaction.ClientAmount:C}");
            sb.AppendLine("--------------------------------------");
            sb.AppendLine($"Remarks: {transaction.Remarks}");
            sb.AppendLine("======================================");

            return sb.ToString();
        }
    }
}
