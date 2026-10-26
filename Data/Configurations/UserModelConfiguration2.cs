using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Data.Configurations
{
    /// <summary>
    /// Fluent configuration for the user-management tables and the new UserMst columns.
    /// Called once from InventoryDbContext.OnModelCreating. All foreign keys use Restrict.
    /// </summary>
    public static class UserModelConfiguration2
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Business2>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.Address).HasMaxLength(300).IsRequired();
                e.Property(x => x.City).HasMaxLength(100);
                e.Property(x => x.Phone).HasMaxLength(50);
                e.Property(x => x.Email).HasMaxLength(150);
                e.Property(x => x.TaxNumber).HasMaxLength(50);
            });

            modelBuilder.Entity<UserRole2>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(50).IsRequired();
                e.Property(x => x.Description).HasMaxLength(200);
                e.HasIndex(x => x.Name).IsUnique();
            });

            modelBuilder.Entity<AppScreen2>(e =>
            {
                e.Property(x => x.ScreenKey).HasMaxLength(100).IsRequired();
                e.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
                e.Property(x => x.GroupName).HasMaxLength(100).IsRequired();
                e.Property(x => x.Route).HasMaxLength(200).IsRequired();
                e.HasIndex(x => x.ScreenKey).IsUnique();
            });

            modelBuilder.Entity<RoleScreenPermission2>(e =>
            {
                e.Property(x => x.ScreenKey).HasMaxLength(100).IsRequired();
                e.HasIndex(x => new { x.RoleId, x.ScreenKey }).IsUnique();
                e.HasOne(x => x.Role).WithMany(r => r.Permissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserMst>(e =>
            {
                // Unique indexes need a bounded column (nvarchar(max) cannot be indexed).
                e.Property(x => x.Email).HasMaxLength(256);
                e.Property(x => x.UserName).HasMaxLength(256);
                e.Property(x => x.PasswordHash).HasMaxLength(500);

                e.Property(x => x.IsActive).HasDefaultValue(true);
                e.Property(x => x.IsRoot).HasDefaultValue(false);

                e.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
                e.HasIndex(x => x.UserName).IsUnique().HasFilter("[UserName] IS NOT NULL");
                // At most one root (owner) user (SQL Server filtered unique index).
                e.HasIndex(x => x.IsRoot).IsUnique().HasFilter("[IsRoot] = 1");

                e.HasOne(x => x.Business).WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
