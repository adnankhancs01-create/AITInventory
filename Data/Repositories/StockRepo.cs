using Common;
using Common.Models.ResponseModel;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Text.Json;

namespace Data.Repositories
{
    public class StockRepo : IStockRepo
    {
        private readonly InventoryDbContext _dbContext;
        private readonly ILogRepository _logRepo;
        public StockRepo(
            InventoryDbContext dbContext,
            ILogRepository logRepo)
        {
            _dbContext = dbContext;
            _logRepo = logRepo;
        }
        public async Task<BaseResponse<VendorStock>> AddEditVendorStockAsync(VendorStock vendorStock)
        {
            try
            {
                if (!vendorStock.StockNumber.HasValue || vendorStock.StockNumber == 0)
                {
                    vendorStock.StockNumber = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                    await _dbContext.VendorStock.AddAsync(new VendorStock
                    {
                        ProductId = vendorStock.ProductId,
                        Quantity = vendorStock.Quantity,
                        StockNumber = vendorStock.StockNumber,
                        TotalPurchasePrice = vendorStock.TotalPurchasePrice,
                        VendorId = vendorStock.VendorId,
                        CreatedOn = DateTime.Now,
                        IsActive = vendorStock.IsActive
                    });
                }
                else
                {
                    var stock = await _dbContext.VendorStock
                        .FirstOrDefaultAsync(x => x.StockNumber == vendorStock.StockNumber);

                    if (stock == null)
                        return BaseResponse<VendorStock>.FailureResponse(new List<string>() { "Something went wrong" }, "Stock not found");

                    stock.Quantity = vendorStock.Quantity;
                    stock.VendorId = vendorStock.VendorId;
                    stock.TotalPurchasePrice = vendorStock.TotalPurchasePrice;
                    stock.ModifiedOn = DateTime.Now;
                    stock.IsActive = vendorStock.IsActive;
                }

                await _dbContext.SaveChangesAsync();
                return BaseResponse<VendorStock>.SuccessResponse(vendorStock, "Request executed successfully");
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(
                    ex,
                    userId: 1,
                    additionalData: JsonSerializer.Serialize(new { message = "Error while adding or updating product" }),
                    request: JsonSerializer.Serialize(vendorStock)
                );

                return BaseResponse<VendorStock>.FailureResponse(
                    new() { ex.Message },
                    "Error occurred while processing request"
                );
            }
        }
        public async Task<(List<VendorStock>, int)> GetStockAsync(int? productId, int pageIndex, int pageSize, string? filter)
        {
            var query = _dbContext.VendorStock
    .AsNoTracking().Include(x => x.Product).ThenInclude(x => x.Category)
                .Where(x => ((productId.HasValue || productId == 0) || x.ProductId == productId) && x.IsActive==true && x.StockNumber.HasValue);

            if (!string.IsNullOrEmpty(filter))
            {
                switch (filter)
                {
                    case string f when f.Contains("ProductName"):
                        filter = filter.Replace("ProductName", "x.Product.Name");
                        break;
                    case string f when f.Contains("ProductCode"):
                        filter = filter.Replace("ProductCode", "x.Product.ProductCode");
                        break;
                    default:
                        break;
                }
                query = query.Where(filter);
            }

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, totalCount);


        }
        public async Task<BaseResponse<bool>> DeductStockAsync( int productId, int quantity)
        {
            // Get all stock entries for the product, ordered oldest first
            var stockList = await _dbContext.VendorStock
                .Where(x => x.ProductId == productId && x.Quantity > 0)
                .OrderBy(x => x.CreatedOn)
                .ToListAsync();

            if (stockList == null || stockList.Count == 0)
                return BaseResponse<bool>.FailureResponse(
                    new List<string> { "Stock not found" },
                    "Stock not found");

            int remaining = quantity;

            foreach (var stock in stockList)
            {
                if (remaining <= 0) break;

                if (stock.Quantity >= remaining)
                {
                    // Deduct only what is needed
                    stock.Quantity -= remaining;
                    stock.ModifiedOn = DateTime.Now;
                    remaining = 0;
                }
                else
                {
                    // Deduct all from this stock and continue
                    remaining -= stock.Quantity??0;
                    stock.Quantity = 0;
                    stock.ModifiedOn = DateTime.Now;
                }
            }

            // If after looping we still have remaining > 0, not enough stock
            if (remaining > 0)
            {
                return BaseResponse<bool>.FailureResponse(
                    new List<string> { "Insufficient stock" },
                    "Not enough stock to fulfill deduction");
            }

            await _dbContext.SaveChangesAsync();

            return BaseResponse<bool>.SuccessResponse(true, "Stock deducted successfully");
        }
        public async Task<(List<StockFifoDto> Data, int TotalCount)> GetFifoStockReport(
    int? productId,
    DateTime? fromDate,
    DateTime? toDate,
    int pageIndex,
    int pageSize)
        {
            // =========================
            // 1. BASE QUERY (FILTER)
            // =========================
            var query = _dbContext.VendorStock.Include(x => x.Product).ThenInclude(x => x.Category)
                .AsNoTracking()
                .Where(x =>
                    x.IsActive == true &&
                    (!productId.HasValue || x.ProductId == productId) &&
                    (!fromDate.HasValue || x.CreatedOn >= fromDate) &&
                    (!toDate.HasValue || x.CreatedOn <= toDate)
                );

            // =========================
            // 2. LOAD DATA (ORDERED)
            // =========================
            var data = await query
                .OrderBy(x => x.ProductId)
                .ThenBy(x => x.CreatedOn)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.ProductId,
                    x.CreatedOn,
                    x.Quantity,
                    ProductName = x.Product.Name,
                    ProductCode = x.Product.ProductCode,
                    CategoryName = x.Product.Category.Name,
                    Id = x.Id,
                    StockNumber = x.StockNumber,
                    TotalPurchasePrice=x.TotalPurchasePrice,
                    ModifiedOn=x.ModifiedOn,
                })
                .ToListAsync();

            // =========================
            // 3. FIFO CALCULATION
            // =========================
            var result = new List<StockFifoDto>();

            foreach (var group in data.GroupBy(x => x.ProductId))
            {
                decimal runningPurchase = 0;

                var sales = group
                    .Where(x => x.Quantity < 0)
                    .Select(x => -x.Quantity)
                    .Sum();

                foreach (var purchase in group.Where(x => x.Quantity > 0))
                {
                    runningPurchase += (purchase.Quantity ?? 0);

                    int soldQty;

                    if (sales <= (runningPurchase - purchase.Quantity))
                        soldQty = 0;
                    else if (sales >= runningPurchase)
                        soldQty = (purchase.Quantity ?? 0);
                    else
                        soldQty = ((int)(sales ?? 0) - (int)(runningPurchase  - (purchase.Quantity ?? 0)));

                    result.Add(new StockFifoDto
                    {
                        ProductId = purchase.ProductId??0,
                        PurchaseDate = purchase.CreatedOn??DateTime.Now,
                        PurchaseQty = purchase.Quantity ?? 0,
                        SoldQty = soldQty,
                        RemainingQty = (purchase.Quantity ?? 0) - soldQty,
                        ProductName = purchase.ProductName,
                        ProductCode = purchase.ProductCode,
                        StockNumber= purchase.StockNumber ?? 0,
                        TotalPurchasePrice = purchase.TotalPurchasePrice,
                        Category=purchase.CategoryName,
                        Id= purchase.Id,
                        ModifiedOn=purchase.ModifiedOn
                    });
                }
            }

            // =========================
            // 4. TOTAL COUNT
            // =========================
            var totalCount = result.Count;

            // =========================
            // 5. PAGINATION
            // =========================
            var pagedData = result
                .OrderBy(x => x.ProductId)
                .ThenBy(x => x.PurchaseDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (pagedData, totalCount);
        }
    }
}
