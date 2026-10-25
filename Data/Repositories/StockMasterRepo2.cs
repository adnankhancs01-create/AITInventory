using Common.Stock2;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class StockMasterRepo2 : IStockMasterRepo2
    {
        private readonly InventoryDbContext _db;

        public StockMasterRepo2(InventoryDbContext db)
        {
            _db = db;
        }

        // ---------------------------------------------------------------- locations

        public async Task<List<StockLocationDto2>> GetLocationDtosAsync(bool includeInactive)
        {
            var query = _db.StockLocations2.AsNoTracking().AsQueryable();
            if (!includeInactive) query = query.Where(l => l.IsActive);

            return await query
                .OrderByDescending(l => l.IsDefault).ThenBy(l => l.Name)
                .Select(l => new StockLocationDto2
                {
                    Id = l.Id,
                    Code = l.Code,
                    Name = l.Name,
                    Address = l.Address,
                    IsDefault = l.IsDefault,
                    IsActive = l.IsActive
                }).ToListAsync();
        }

        public async Task<bool> LocationCodeExistsAsync(string code, int excludeLocationId)
        {
            return await _db.StockLocations2.AnyAsync(l => l.Code == code && l.Id != excludeLocationId);
        }

        public async Task<bool> LocationHoldsStockAsync(int locationId)
        {
            return await _db.StockLocationBalances2.AnyAsync(b => b.LocationId == locationId && b.Quantity > 0);
        }

        public async Task<bool> LocationHasTransfersInTransitAsync(int locationId)
        {
            return await _db.StockTransfers2.AnyAsync(t => t.Status == StockConstants2.StatusInTransit
                && (t.FromLocationId == locationId || t.ToLocationId == locationId));
        }

        public async Task<bool> AnyStockAllocatedToOtherLocationsAsync()
        {
            return await _db.StockLocationBalances2.AnyAsync(b => b.Quantity > 0);
        }

        public async Task<bool> AnyTransferInTransitAsync()
        {
            return await _db.StockTransfers2.AnyAsync(t => t.Status == StockConstants2.StatusInTransit);
        }

        public async Task<List<StockLocation2>> GetOtherDefaultLocationsAsync(int exceptLocationId)
        {
            return await _db.StockLocations2.Where(l => l.IsDefault && l.Id != exceptLocationId).ToListAsync();
        }

        public void AddLocation(StockLocation2 location)
        {
            _db.StockLocations2.Add(location);
        }

        // ---------------------------------------------------------------- suppliers

        public async Task<List<StockSupplierDto2>> GetSupplierDtosAsync(bool includeInactive)
        {
            var query = _db.Vendor.AsNoTracking().AsQueryable();
            if (!includeInactive) query = query.Where(v => v.IsActive != false);

            return await query.OrderBy(v => v.Name).Select(v => new StockSupplierDto2
            {
                Id = v.Id,
                Name = v.Name,
                Address = v.Address,
                City = v.City,
                State = v.State,
                PostalCode = v.PostalCode,
                Country = v.Country,
                IsActive = v.IsActive != false
            }).ToListAsync();
        }

        public async Task<bool> SupplierNameExistsAsync(string name, int excludeSupplierId)
        {
            return await _db.Vendor.AnyAsync(v => v.Name == name && v.Id != excludeSupplierId);
        }

        public async Task<Vendor?> GetSupplierAsync(int supplierId)
        {
            return await _db.Vendor.FirstOrDefaultAsync(v => v.Id == supplierId);
        }

        public async Task<Vendor?> FindSupplierAsync(int supplierId)
        {
            return await _db.Vendor.AsNoTracking().FirstOrDefaultAsync(v => v.Id == supplierId);
        }

        public void AddSupplier(Vendor supplier)
        {
            _db.Vendor.Add(supplier);
        }

        // ---------------------------------------------------------------- reorder settings

        public async Task<Dictionary<int, ProductStockSetting2>> GetSettingsByProductAsync()
        {
            return await _db.ProductStockSettings2.AsNoTracking().ToDictionaryAsync(s => s.ProductId);
        }

        public async Task<ProductStockSetting2?> GetSettingAsync(int productId)
        {
            return await _db.ProductStockSettings2.FirstOrDefaultAsync(s => s.ProductId == productId);
        }

        public async Task<List<ProductStockSetting2>> GetReorderSettingsAsync()
        {
            return await _db.ProductStockSettings2.AsNoTracking().Where(s => s.ReorderLevel > 0).ToListAsync();
        }

        public void AddSetting(ProductStockSetting2 setting)
        {
            _db.ProductStockSettings2.Add(setting);
        }

        public async Task<bool> ActiveProductExistsAsync(int productId)
        {
            return await _db.Products.AnyAsync(p => p.Id == productId && p.IsActive);
        }

        public async Task<Dictionary<int, string>> GetSupplierNamesAsync()
        {
            return await _db.Vendor.AsNoTracking().ToDictionaryAsync(v => v.Id, v => v.Name);
        }
    }
}
