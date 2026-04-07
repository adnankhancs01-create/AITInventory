using Common;
using Common.Models;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.IRepositories
{
    public interface IProductRepo
    {
        Task<(List<Product>, int)> GetProductsAsync(int id, int pageIndex, int pageSize, string? Filter);
        Task<(List<ProductCategory>, int)> GetProductCategoriesAsync(int id, int pageIndex, int pageSize, string? Filter);
        Task<BaseResponse<Product>> AddEditProductAsync(Product product);
        Task<BaseResponse<ProductCategory>> AddEditProductCategoryAsync(ProductCategory productCategory);
        Task<BaseResponse<VendorStock>> AddEditVendorStockAsync(VendorStock vendorStock);
        Task<BaseResponse<Pricing>> AddPricingAsync(Pricing pricing);
        Task<List<Pricing>> GetPricingByProductCodeAsync(List<string>? productCodes);
    }
}
