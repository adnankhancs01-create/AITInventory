using Domain.Stock2;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    /// <summary>Saves changes and translates EF Core's concurrency exception into the domain's StockConcurrencyException2.</summary>
    internal static class StockDbSave2
    {
        public static async Task SaveAsync(InventoryDbContext db)
        {
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new StockConcurrencyException2(ex);
            }
        }
    }
}
