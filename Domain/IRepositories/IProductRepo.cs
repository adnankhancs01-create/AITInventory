using Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.IRepositories
{
    public interface IProductRepo
    {
        Task<List<Product>> GetProductsAsync(int? id);
        Task<List<ProductCategory>> GetProductCategoriesAsync(int? id);
        Task<BaseResponse<Product>> AddEditProductAsync(Product product);
        Task<BaseResponse<ProductCategory>> AddEditProductCategoryAsync(ProductCategory productCategory);
    }
}
