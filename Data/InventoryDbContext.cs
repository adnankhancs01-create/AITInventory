using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data
{
    public class InventoryDbContext:DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Vendor> Vendor { get; set; }
        public DbSet<VendorClientDetail> VendorClientDetail { get; set; }
        public DbSet<VendorClient> VendorClient { get; set; }
        public DbSet<VendorStock> VendorStock { get; set; }
        public DbSet<VendorTransaction> VendorTransaction { get; set; }
        public DbSet<ExceptionLog> ExceptionLogs { get; set; }
    }
}
