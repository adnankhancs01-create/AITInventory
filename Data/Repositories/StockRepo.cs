using Common;
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
                    await _dbContext.VendorStock.AddAsync(new VendorStock
                    {
                        ProductId = vendorStock.ProductId,
                        Quantity = vendorStock.Quantity,
                        StockNumber = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff")),
                        TotalPurchasePrice = vendorStock.TotalPurchasePrice,
                        VendorId = vendorStock.VendorId,
                        CreatedOn = DateTime.Now
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
                .Where(x => (productId.HasValue || productId == 0) || x.ProductId == productId);

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

    }
}
