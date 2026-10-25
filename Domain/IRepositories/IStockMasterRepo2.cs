using Common.Stock2;
using Domain.Entities;

namespace Domain.IRepositories
{
    /// <summary>Data access for locations, suppliers (the existing Vendor table) and reorder settings.</summary>
    public interface IStockMasterRepo2
    {
        // ---------------------------------------------------------------- locations
        Task<List<StockLocationDto2>> GetLocationDtosAsync(bool includeInactive);
        Task<bool> LocationCodeExistsAsync(string code, int excludeLocationId);
        Task<bool> LocationHoldsStockAsync(int locationId);
        Task<bool> LocationHasTransfersInTransitAsync(int locationId);
        Task<bool> AnyStockAllocatedToOtherLocationsAsync();
        Task<bool> AnyTransferInTransitAsync();
        Task<List<StockLocation2>> GetOtherDefaultLocationsAsync(int exceptLocationId);
        void AddLocation(StockLocation2 location);

        // ---------------------------------------------------------------- suppliers
        Task<List<StockSupplierDto2>> GetSupplierDtosAsync(bool includeInactive);
        Task<bool> SupplierNameExistsAsync(string name, int excludeSupplierId);

        /// <summary>Tracked supplier (can be modified and saved).</summary>
        Task<Vendor?> GetSupplierAsync(int supplierId);

        /// <summary>Read-only supplier lookup.</summary>
        Task<Vendor?> FindSupplierAsync(int supplierId);
        void AddSupplier(Vendor supplier);

        // ---------------------------------------------------------------- reorder settings
        Task<Dictionary<int, ProductStockSetting2>> GetSettingsByProductAsync();
        Task<ProductStockSetting2?> GetSettingAsync(int productId);
        Task<List<ProductStockSetting2>> GetReorderSettingsAsync();
        void AddSetting(ProductStockSetting2 setting);
        Task<bool> ActiveProductExistsAsync(int productId);
        Task<Dictionary<int, string>> GetSupplierNamesAsync();
    }
}
