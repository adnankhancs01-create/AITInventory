using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories
{
    public class ProductRepo: IProductRepo
    {
        private readonly InventoryDbContext _dbContext;
        public ProductRepo(InventoryDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<List<Product>> GetProductsAsync(int? id)
        {
            return await _dbContext.Products
                .Where(x => id.HasValue ? id == x.Id : 1 == 1)
                .Include(x => x.Category)
                .ToListAsync();
        }
        public async Task<List<ProductCategory>> GetProductCategoriesAsync(int? id)
        {
            return await _dbContext.ProductCategories
                .Where(x => id.HasValue ? id == x.Id : 1 == 1)
                .ToListAsync();
        }
    }
}
