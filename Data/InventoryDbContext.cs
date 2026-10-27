using Data.Configurations;
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
        public DbSet<ClientDetail> ClientDetail { get; set; }
        public DbSet<Stock> Stock { get; set; }
        //public DbSet<VendorTransaction> VendorTransaction { get; set; }
        public DbSet<ExceptionLog> ExceptionLogs { get; set; }
        public DbSet<UserMst> UserMst { get; set; }
        public DbSet<Pricing> Pricing { get; set; }
        public DbSet<TransactionSlip> TransactionSlip { get; set; }
        public DbSet<TransactionMst> TransactionMst { get; set; }
        public DbSet<TransactionDetails> TransactionDetails { get; set; }
        public DbSet<ReturnTransaction> ReturnTransaction { get; set; }

        // ---------- Stock management module (all new, all suffixed 2) ----------
        public DbSet<StockLocation2> StockLocations2 { get; set; }
        public DbSet<StockLocationBalance2> StockLocationBalances2 { get; set; }
        public DbSet<StockBatch2> StockBatches2 { get; set; }
        public DbSet<ProductStockSetting2> ProductStockSettings2 { get; set; }
        public DbSet<StockMovement2> StockMovements2 { get; set; }
        public DbSet<PurchaseOrder2> PurchaseOrders2 { get; set; }
        public DbSet<PurchaseOrderLine2> PurchaseOrderLines2 { get; set; }
        public DbSet<StockReceipt2> StockReceipts2 { get; set; }
        public DbSet<StockReceiptLine2> StockReceiptLines2 { get; set; }
        public DbSet<StockIssue2> StockIssues2 { get; set; }
        public DbSet<StockIssueLine2> StockIssueLines2 { get; set; }
        public DbSet<StockTransfer2> StockTransfers2 { get; set; }
        public DbSet<StockTransferLine2> StockTransferLines2 { get; set; }
        public DbSet<StockAdjustment2> StockAdjustments2 { get; set; }
        public DbSet<StockAdjustmentLine2> StockAdjustmentLines2 { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Decimal precision
            modelBuilder.Entity<TransactionDetails>()
                .Property(t => t.UnitPrice)
                .HasColumnType("decimal(18,2)");

            //modelBuilder.Entity<TransactionMst>()
            //    .Property(t => t.NetAmount)
            //    .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Stock>()
                .Property(v => v.TotalPurchasePrice)
                .HasColumnType("decimal(18,2)");

            // Relationships
            modelBuilder.Entity<Stock>()
                .HasOne(vs => vs.Product)
                .WithMany(p => p.Stocks)
                .HasForeignKey(vs => vs.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Stock>()
                .HasOne(vs => vs.Product)
                .WithMany(p => p.Stocks)
                .HasForeignKey(vs => vs.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Stock management module (new tables only; existing tables are not altered)
            StockModelConfiguration2.Configure(modelBuilder);
            UserModelConfiguration2.Configure(modelBuilder);
            RetailPosModelConfiguration2.Configure(modelBuilder);
        }
    }
}
