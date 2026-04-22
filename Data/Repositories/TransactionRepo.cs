using Common;
using Common.Models.RequestModel;
using Common.Models.ResponseModel;
using Dapper;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;

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

            // 1. Detail records
            var data = (await multi.ReadAsync<TransactionDetailModel>()).ToList();

            // 2. Summary per transaction
            var summaryData = (await multi.ReadAsync<TransactionSummaryDataModel>()).ToList();

            // 3. Total count
            var totalCount = await multi.ReadFirstAsync<int>();

            // 4. Dashboard summary
            var summary = await multi.ReadFirstAsync<TransactionSummaryModel>();

            return new PagedTransactionResponse
            {
                Data = data,
                SummaryData = summaryData,
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
        public async Task<BaseResponse<int?>> CreateReturnTransaction(
    int transactionId,
    int productId,
    int quantity,
    decimal amount,
    int stockQty,
    int createdBy)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var item = await _dbContext.TransactionDetails
                    .FirstOrDefaultAsync(x => x.TransMstId == transactionId && x.ProductId == productId);

                if (item == null)
                    return BaseResponse<int?>.FailureResponse(new List<string> { "Item not found" }, "Error");

                // 🧾 Insert return
                var returnEntity = new ReturnTransaction
                {
                    TransactionId = transactionId,
                    TransactionDetailId = item.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    Amount = amount,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                await _dbContext.ReturnTransaction.AddAsync(returnEntity);

                // 🔄 Update returned qty
                item.ReturnedQuantity = (item.ReturnedQuantity ?? 0) + quantity;

                // 📦 Add stock entry
                var stock = new VendorStock
                {
                    ProductId = productId,
                    Quantity = stockQty,
                    TransactionId = item.Id,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = createdBy,
                    IsActive = true
                };

                await _dbContext.VendorStock.AddAsync(stock);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return BaseResponse<int?>.SuccessResponse(returnEntity.Id, "Return successful");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BaseResponse<int?>.FailureResponse(new List<string> { ex.Message }, "Error occurred");
            }
        }

        public async Task<TransactionDetails?> GetTransactionDetailByProduct(int transactionId, int productId)
        {
            return await _dbContext.TransactionDetails.Include(x=> x.TransMst).AsNoTracking()
                .FirstOrDefaultAsync(x => x.TransMstId == transactionId && x.ProductId == productId);
        }
    }
}
