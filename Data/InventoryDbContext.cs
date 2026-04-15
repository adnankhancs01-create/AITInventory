using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data
{
    public class InventoryDbContext  : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Vendor> Vendor { get; set; }
        public DbSet<VendorClientDetail> VendorClientDetail { get; set; }
        public DbSet<VendorClient> VendorClient { get; set; }
        public DbSet<VendorStock> VendorStock { get; set; }
        //public DbSet<VendorTransaction> VendorTransaction { get; set; }
        public DbSet<ExceptionLog> ExceptionLogs { get; set; }
        public DbSet<UserMst> UserMst { get; set; }
        public DbSet<Pricing> Pricing { get; set; }
        public DbSet<TransactionSlip> TransactionSlip { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Decimal precision
            modelBuilder.Entity<TransactionDetails>()
                .Property(t => t.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<TransactionMst>()
                .Property(t => t.NetAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<VendorStock>()
                .Property(v => v.TotalPurchasePrice)
                .HasColumnType("decimal(18,2)");

            // Relationships
            modelBuilder.Entity<VendorStock>()
                .HasOne(vs => vs.Product)
                .WithMany(p => p.Stocks)
                .HasForeignKey(vs => vs.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VendorStock>()
                .HasOne(vs => vs.Product)
                .WithMany(p => p.Stocks)
                .HasForeignKey(vs => vs.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
