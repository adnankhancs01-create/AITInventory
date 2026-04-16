using Common;
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
                _dbContext.TransactionSlip.Update(transactionSlip);
            else
                await _dbContext.TransactionSlip.AddAsync(transactionSlip);

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
            await _dbContext.AddAsync(transactionMst);
            await _dbContext.SaveChangesAsync();

            var stocks = new List<VendorStock>();
            transactionMst.TransactionDetails.ToList().ForEach(detail =>
            {
                var stock = new VendorStock
                {
                    ProductId = detail.ProductId,
                    Quantity = transactionMst.TransactionType == "Purchase" ? detail.Quantity : -detail.Quantity,
                    TransactionId = transactionMst.Id,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = transactionMst.CreatedBy
                };
                stocks.Add(stock);
            });

            if (stocks.Count > 0)
            { await _dbContext.VendorStock.AddRangeAsync(stocks);
                await _dbContext.SaveChangesAsync();
            }
            return BaseResponse<int?>.SuccessResponse(transactionMst.Id, "Transaction processed successfully");
        }
        public async Task<PagedTransactionResponse> GetTransactionsAsync(TransactionFilterRequest request)
        {
            using var con = new SqlConnection(_connectionString);

            var multi = await con.QueryMultipleAsync(
                "sp_GetTransactions_Paged",
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
    }
}
