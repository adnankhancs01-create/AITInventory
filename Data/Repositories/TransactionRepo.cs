using Common;
using Common.Models;
using Common.Models.RequestModel;
using Common.Models.ResponseModel;
using Dapper;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Data.Repositories
{
    public class TransactionRepo : ITransactionRepo
    {
        private readonly InventoryDbContext _dbContext;
        private readonly string _connectionString;
        public TransactionRepo(InventoryDbContext dbContext, IConfiguration config)
        {
            _dbContext = dbContext;
            _connectionString = config.GetConnectionString("DefaultConnection");
        }
        //public async Task<VendorTransaction> AddEditTransactionAsync(VendorTransaction transaction)
        //{
        //    if (transaction.Id > 0)
        //        _dbContext.VendorTransaction.Update(transaction);
        //    else
        //        await _dbContext.VendorTransaction.AddAsync(transaction);

        //    await _dbContext.SaveChangesAsync();

        //    return transaction;
        //}
        public async Task<TransactionSlip> AddEditTransactionSlipAsync(TransactionSlip transactionSlip)
        {
            if (transactionSlip.Id > 0)
            {
                _dbContext.Update(transactionSlip);
            }
            else
                await _dbContext.AddAsync(transactionSlip);

            await _dbContext.SaveChangesAsync();

            return transactionSlip;
        }

        //public async Task<(List<VendorTransaction>, int)> GetTransactionAsync(int? transactionId, int pageIndex, int pageSize, string? filter)
        //{
        //    var query = _dbContext.VendorTransaction
        //        .Include(t => t.Client)
        //        .Include(t => t.Product)
        //        .AsQueryable();
        //    if (transactionId.HasValue && transactionId>0)
        //        query = query.Where(t => t.Id == transactionId.Value);
        //    if (!string.IsNullOrEmpty(filter))
        //        query = query.Where(t => t.Client.FirstName.Contains(filter) || t.Client.LastName.Contains(filter) || t.Product.Name.Contains(filter));
        //    var totalRecords = await query.CountAsync();
        //    var transactions = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
        //    return (transactions, totalRecords);
        //}

        public async Task<BaseResponse<int?>> ProcessTransactions(TransactionMst transactionMst)
        {
            if (transactionMst.Id < 0)
                return BaseResponse<int?>.FailureResponse(new List<string> { "Failed to save transaction" }, "Transaction processing failed");

            if (transactionMst.Id > 0)
            {
                _dbContext.ChangeTracker.Clear(); 
                _dbContext.Update(transactionMst);
            }
            else
                await _dbContext.AddAsync(transactionMst);
            await _dbContext.SaveChangesAsync();

            var stocks = new List<VendorStock>();
            var listTransactionDetailId = transactionMst.TransactionDetails.ToList().Select(x=>x.Id).ToList();
            var getStocks = await _dbContext.VendorStock.Where(s => listTransactionDetailId.Contains(s.TransactionId ?? 0)).ToListAsync();
            transactionMst.TransactionDetails.ToList().ForEach(detail =>
            {
                // inactive recent stock
                if (getStocks != null && getStocks.Count > 0)
                {
                    var stocksToUpdate = getStocks.Where(s => s.TransactionId == detail.Id).ToList();
                    if (stocksToUpdate != null && stocksToUpdate.Count > 0)
                    {
                        stocksToUpdate.ForEach(s => s.IsActive = false);
                    }
                }

                // add new stock record
                var stock = new VendorStock
                {
                    ProductId = detail.ProductId,
                    Quantity = transactionMst.TransactionType == "Purchase" ? detail.Quantity : -detail.Quantity,
                    TransactionId = detail.Id,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = transactionMst.CreatedBy,
                    IsActive= true
                };
                stocks.Add(stock);
            });

            if (stocks.Count > 0)
            {
                await _dbContext.AddRangeAsync(stocks);
            }
            await _dbContext.SaveChangesAsync();
            return BaseResponse<int?>.SuccessResponse(transactionMst.Id, "Transaction processed successfully");
        }
        public async Task<PagedTransactionResponse> GetTransactionsAsync(TransactionFilterRequest request)
        {
            using var con = new SqlConnection(_connectionString);

            var multi = await con.QueryMultipleAsync(
                "sp_GetTransactions",
                new
                {
                    request.FromDate,
                    request.ToDate,
                    request.ProductId,
                    request.CategoryId,
                    request.ClientId,
                    request.TransactionType,
                    request.PageNumber,
                    request.PageSize
                },
                commandType: CommandType.StoredProcedure
            );

            var data = (await multi.ReadAsync<TransactionDetailModel>()).ToList();
            var totalCount = await multi.ReadFirstAsync<int>();
            var summary = await multi.ReadFirstAsync<TransactionSummaryModel>();

            return new PagedTransactionResponse
            {
                Data = data,
                TotalCount = totalCount,
                Summary = summary
            };
        }

        public async Task<TransactionMst> GetTransactionByIdAsync(int transactionId)
        {
            // Load master + details in one query
            var transaction = await _dbContext.TransactionMst.AsNoTracking()
                .Include(t => t.TransactionDetails).ThenInclude(d => d.Product).AsNoTracking() // Include product details for name and unit price
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return null;

            return transaction;
        }

    }
}
