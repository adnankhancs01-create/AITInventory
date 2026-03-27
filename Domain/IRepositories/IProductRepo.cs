using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.IRepositories
{
    public interface IProductRepo
    {
        Task<List<Product>> GetProductsAsync(int? id);
        Task<List<ProductCategory>> GetProductCategoriesAsync(int? id);
    }
}
