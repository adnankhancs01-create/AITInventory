using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Data.Configurations
{
    /// <summary>
    /// Fluent configuration for every Stock2 entity. Called once from InventoryDbContext.OnModelCreating.
    /// All relationships to existing tables use DeleteBehavior.Restrict so existing data can never be
    /// removed by a cascade, and so SQL Server never reports "multiple cascade paths".
    /// </summary>
    public static class StockModelConfiguration2
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            // ---------- Locations ----------
            modelBuilder.Entity<StockLocation2>(e =>
            {
                e.HasIndex(x => x.Code).IsUnique();
                // At most one default location (SQL Server filtered unique index).
                e.HasIndex(x => x.IsDefault).IsUnique().HasFilter("[IsDefault] = 1");
            });

            modelBuilder.Entity<StockLocationBalance2>(e =>
            {
                e.HasIndex(x => new { x.ProductId, x.LocationId }).IsUnique();
                e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- Batch / settings / movements ----------
            modelBuilder.Entity<StockBatch2>(e =>
            {
                e.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
                e.HasIndex(x => x.VendorStockId).IsUnique();
                e.HasIndex(x => new { x.ProductId, x.ExpiryDate });
                e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.VendorStock).WithMany().HasForeignKey(x => x.VendorStockId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProductStockSetting2>(e =>
            {
                e.HasIndex(x => x.ProductId).IsUnique();
                e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.PreferredVendor).WithMany().HasForeignKey(x => x.PreferredVendorId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StockMovement2>(e =>
            {
                e.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
                e.HasIndex(x => new { x.ProductId, x.CreatedOn });
                e.HasIndex(x => x.VendorStockId);
                e.HasIndex(x => new { x.DocumentType, x.DocumentId });
                e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.VendorStock).WithMany().HasForeignKey(x => x.VendorStockId).OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- Purchase orders ----------
            modelBuilder.Entity<PurchaseOrder2>(e =>
            {
                e.HasIndex(x => x.PoNumber).IsUnique();
                e.HasIndex(x => x.Status);
                e.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(x => x.Lines).WithOne(x => x.PurchaseOrder).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PurchaseOrderLine2>(e =>
            {
                e.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
                e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- Receipts ----------
            modelBuilder.Entity<StockReceipt2>(e =>
            {
                e.HasIndex(x => x.ReceiptNumber).IsUnique();
                e.HasIndex(x => x.ReceiptDate);
                e.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(x => x.Lines).WithOne(x => x.Receipt).HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StockReceiptLine2>(e =>
            {
                e.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
                e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.PurchaseOrderLine).WithMany().HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.VendorStock).WithMany().HasForeignKey(x => x.VendorStockId).OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- Issues ----------
            modelBuilder.Entity<StockIssue2>(e =>
            {
                e.HasIndex(x => x.IssueNumber).IsUnique();
                e.HasIndex(x => x.IssueDate);
                e.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(x => x.Lines).WithOne(x => x.Issue).HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StockIssueLine2>(e =>
            {
                e.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
                e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.VendorStock).WithMany().HasForeignKey(x => x.VendorStockId).OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- Transfers ----------
            modelBuilder.Entity<StockTransfer2>(e =>
            {
                e.HasIndex(x => x.TransferNumber).IsUnique();
                e.HasIndex(x => x.Status);
                e.HasOne(x => x.FromLocation).WithMany().HasForeignKey(x => x.FromLocationId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.ToLocation).WithMany().HasForeignKey(x => x.ToLocationId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(x => x.Lines).WithOne(x => x.Transfer).HasForeignKey(x => x.TransferId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StockTransferLine2>(e =>
            {
                e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- Adjustments ----------
            modelBuilder.Entity<StockAdjustment2>(e =>
            {
                e.HasIndex(x => x.AdjustmentNumber).IsUnique();
                e.HasIndex(x => x.AdjustmentDate);
                e.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(x => x.Lines).WithOne(x => x.Adjustment).HasForeignKey(x => x.AdjustmentId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StockAdjustmentLine2>(e =>
            {
                e.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
                e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.VendorStock).WithMany().HasForeignKey(x => x.VendorStockId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
