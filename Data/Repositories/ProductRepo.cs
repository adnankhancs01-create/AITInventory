using Common;
using Common.Models;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Data.Repositories
{
    public class ProductRepo: IProductRepo
    {
        private readonly InventoryDbContext _dbContext;
        private readonly ILogRepository _logRepo;
        public ProductRepo(
            InventoryDbContext dbContext,
            ILogRepository logRepo)
        {
            _dbContext = dbContext;
            _logRepo = logRepo;
        }
        public async Task<(List<Product>, int)> GetProductsAsync(int id, int pageIndex, int pageSize, string? Filter)
        {
            var query = _dbContext.Products
                .Where(x => x.Id == id || id == 0);

            if (!string.IsNullOrEmpty(Filter))
                query = query.Where(Filter);

            var totalCount = await query.CountAsync();

            var data = await query.Include(x=> x.Category).Include(x=> x.Stock)
                .OrderByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, totalCount);


        }
        public async Task<(List<ProductCategory>, int)> GetProductCategoriesAsync(int id, int pageIndex, int pageSize, string? Filter)
        {
            var query = _dbContext.ProductCategories
                .Where(x => x.Id == id || id == 0);

            if (!string.IsNullOrEmpty(Filter))
                query = query.Where(Filter);

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, totalCount);


        }
        //public async Task<List<ProductCategory>> GetProductCategoriesAsync(int? id)
        //{
        //    return await _dbContext.ProductCategories
        //        .Where(x => id.HasValue ? id == x.Id : 1 == 1)
        //        .ToListAsync();
        //}
        public async Task<BaseResponse<Product>> AddEditProductAsync(Product product)
        {
            try
            {
                if (product.Id > 0)
                {
                    var getProduct = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == product.Id);
                    getProduct.Name=product.Name;
                    getProduct.Description=product.Description;
                    getProduct.CategoryId=product.CategoryId;
                    getProduct.ModifiedOn=product.ModifiedOn;
                }
                else
                {
                    // Generate Product Code
                    product.ProductCode = await GenerateProductCodeAsync(product.CategoryId);
                    await _dbContext.Products.AddAsync(product);
                }

                await _dbContext.SaveChangesAsync();

                return BaseResponse<Product>.SuccessResponse(product, "Request executed successfully");
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(
                    ex,
                    userId: product.CreatedBy ?? product.ModifiedBy ?? 0,
                    additionalData: JsonSerializer.Serialize(new { message = "Error while adding or updating product" }),
                    request: JsonSerializer.Serialize(product)
                );

                return BaseResponse<Product>.FailureResponse(
                    new() { ex.Message },
                    "Error occurred while processing request"
                );
            }
        }

        public async Task<BaseResponse<ProductCategory>> AddEditProductCategoryAsync(ProductCategory productCategory)
        {
            try
            {
                if (productCategory.Id > 0)
                {
                    _dbContext.ProductCategories.Update(productCategory);
                }
                else
                {
                    int recentId=await _dbContext.ProductCategories.OrderByDescending(x => x.Id).Select(x => x.Id).FirstOrDefaultAsync();
                    int incrementalId= recentId + 1;
                    productCategory.Id = incrementalId;
                    await _dbContext.ProductCategories.AddAsync(productCategory);
                }

                await _dbContext.SaveChangesAsync();

                return BaseResponse<ProductCategory>.SuccessResponse(productCategory, "Request executed successfully");
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(
                    ex,
                    userId: productCategory.CreatedBy ?? productCategory.ModifiedBy ?? 0,
                    additionalData: JsonSerializer.Serialize(new { message = "Error while adding or updating product" }),
                    request: JsonSerializer.Serialize(productCategory)
                );

                return BaseResponse<ProductCategory>.FailureResponse(
                    new() { ex.Message },
                    "Error occurred while processing request"
                );
            }
        }

        private async Task<string> GenerateProductCodeAsync(int categoryId)
        {
            var category = await _dbContext.ProductCategories
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category == null)
                throw new Exception("Category not found");

            // Take first 3 letters of category
            string prefix = category.Name.Substring(0, Math.Min(3, category.Name.Length)).ToUpper();

            // Get last product in this category
            var lastProduct = await _dbContext.Products
                .Where(p => p.CategoryId == categoryId)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastProduct != null && !string.IsNullOrEmpty(lastProduct.ProductCode))
            {
                var lastNumberPart = lastProduct.ProductCode.Split('-').Last();

                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}-{nextNumber:D3}";
        }
        public async Task<BaseResponse<VendorStock>> AddEditVendorStockAsync(VendorStock vendorStock)
        {
            try
            {
                var stock = await _dbContext.VendorStock
                    .FirstOrDefaultAsync(x => x.ProductId == vendorStock.ProductId);

                if (stock == null)
                {
                    stock = vendorStock;
                    await _dbContext.VendorStock.AddAsync(stock);
                }
                else
                {
                    stock.Quantity = vendorStock.Quantity;
                    stock.UnitPrice = vendorStock.UnitPrice;
                    stock.SellPrice = vendorStock.SellPrice;
                    stock.VendorId = vendorStock.VendorId;
                }

                await _dbContext.SaveChangesAsync();
                return BaseResponse<VendorStock>.SuccessResponse(stock, "Request executed successfully");
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


    }
}
