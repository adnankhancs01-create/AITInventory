using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Data.Configurations
{
    /// <summary>
    /// Fluent configuration for the Retail POS tables (RetailPos2_*). Called once from InventoryDbContext.OnModelCreating.
    /// Every foreign key is Restrict: financial records are never removed by a cascade.
    /// </summary>
    public static class RetailPosModelConfiguration2
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RetailPosClient2>(e =>
            {
                e.ToTable("RetailPos2_Clients");
                e.Property(x => x.Name).HasMaxLength(150).IsRequired();
                e.Property(x => x.Phone).HasMaxLength(30);
                e.Property(x => x.Address).HasMaxLength(300);
                e.Property(x => x.OpeningBalance).HasColumnType("decimal(18,2)");
                e.Property(x => x.CreditLimit).HasColumnType("decimal(18,2)");
                e.Property(x => x.RowVersion).IsRowVersion();
                e.HasIndex(x => x.Name);
                e.HasIndex(x => x.Phone);
            });

            modelBuilder.Entity<RetailPosSale2>(e =>
            {
                e.ToTable("RetailPos2_Sales", t =>
                {
                    t.HasCheckConstraint("CK_RetailPos2_Sales_Total", "[TotalAmount] >= 0");
                });
                e.Property(x => x.InvoiceNumber).HasMaxLength(30).IsRequired();
                e.Property(x => x.PaymentType).HasMaxLength(20).IsRequired();
                e.Property(x => x.Remarks).HasMaxLength(500);
                e.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
                e.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
                e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
                e.Property(x => x.RowVersion).IsRowVersion();
                e.HasIndex(x => x.InvoiceNumber).IsUnique();
                e.HasIndex(x => x.IdempotencyKey).IsUnique();
                e.HasIndex(x => new { x.ClientId, x.SaleDate });
                e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RetailPosSaleLine2>(e =>
            {
                e.ToTable("RetailPos2_SaleLines", t =>
                {
                    t.HasCheckConstraint("CK_RetailPos2_SaleLines_Quantity", "[Quantity] > 0");
                });
                e.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
                e.Property(x => x.ProductName).HasMaxLength(150).IsRequired();
                e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
                e.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
                e.HasIndex(x => x.SaleId);
                e.HasOne(x => x.Sale).WithMany(s => s.Lines).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RetailPosPayment2>(e =>
            {
                e.ToTable("RetailPos2_Payments", t =>
                {
                    t.HasCheckConstraint("CK_RetailPos2_Payments_Amount", "[Amount] > 0");
                });
                e.Property(x => x.PaymentNumber).HasMaxLength(30).IsRequired();
                e.Property(x => x.Method).HasMaxLength(20).IsRequired();
                e.Property(x => x.Reference).HasMaxLength(100);
                e.Property(x => x.Remarks).HasMaxLength(250);
                e.Property(x => x.ReversalReason).HasMaxLength(250);
                e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
                e.HasIndex(x => x.PaymentNumber).IsUnique();
                e.HasIndex(x => x.IdempotencyKey).IsUnique();
                e.HasIndex(x => x.SaleId);
                e.HasIndex(x => new { x.ClientId, x.PaymentDate });
                e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Sale).WithMany(s => s.Payments).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RetailPosAdjustment2>(e =>
            {
                e.ToTable("RetailPos2_Adjustments", t =>
                {
                    t.HasCheckConstraint("CK_RetailPos2_Adjustments_Amount", "[Amount] <> 0");
                });
                e.Property(x => x.AdjustmentType).HasMaxLength(20).IsRequired();
                e.Property(x => x.Reason).HasMaxLength(250).IsRequired();
                e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
                e.HasIndex(x => x.ClientId);
                e.HasIndex(x => x.SaleId);
                e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Sale).WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RetailPosReturn2>(e =>
            {
                e.ToTable("RetailPos2_Returns", t =>
                {
                    t.HasCheckConstraint("CK_RetailPos2_Returns_Amounts", "[CreditAmount] >= 0 AND [RefundAmount] >= 0");
                });
                e.Property(x => x.ReturnNumber).HasMaxLength(30).IsRequired();
                e.Property(x => x.RefundMethod).HasMaxLength(20);
                e.Property(x => x.Reason).HasMaxLength(250).IsRequired();
                e.Property(x => x.CreditAmount).HasColumnType("decimal(18,2)");
                e.Property(x => x.RefundAmount).HasColumnType("decimal(18,2)");
                e.HasIndex(x => x.ReturnNumber).IsUnique();
                e.HasIndex(x => x.IdempotencyKey).IsUnique();
                e.HasIndex(x => x.SaleId);
                e.HasIndex(x => new { x.ClientId, x.ReturnDate });
                e.HasOne(x => x.Sale).WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RetailPosReturnLine2>(e =>
            {
                e.ToTable("RetailPos2_ReturnLines", t =>
                {
                    t.HasCheckConstraint("CK_RetailPos2_ReturnLines_Quantity", "[Quantity] > 0");
                });
                e.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
                e.Property(x => x.ProductName).HasMaxLength(150).IsRequired();
                e.Property(x => x.StockAction).HasMaxLength(30).IsRequired();
                e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
                e.Property(x => x.LineAmount).HasColumnType("decimal(18,2)");
                e.HasIndex(x => x.ReturnId);
                e.HasIndex(x => x.SaleLineId);
                e.HasOne(x => x.Return).WithMany(r => r.Lines).HasForeignKey(x => x.ReturnId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.SaleLine).WithMany().HasForeignKey(x => x.SaleLineId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<StockLocation2>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
