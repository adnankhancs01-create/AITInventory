using Common;
using Common.Stock2;
using Data;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.IService;

namespace Service.Service
{
    public class StockMasterService2 : IStockMasterService2
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogRepository _logRepo;

        public StockMasterService2(IServiceScopeFactory scopeFactory, ILogRepository logRepo)
        {
            _scopeFactory = scopeFactory;
            _logRepo = logRepo;
        }

        // ------------------------------------------------------------------ locations

        public Task<BaseResponse<List<StockLocationDto2>>> GetLocationsAsync(bool includeInactive)
        {
            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                await StockLedgerHelper2.EnsureDefaultLocationAsync(db);

                var query = db.StockLocations2.AsNoTracking().AsQueryable();
                if (!includeInactive) query = query.Where(l => l.IsActive);

                var list = await query
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

                return StockResult2.Ok(list);
            }, _logRepo, nameof(GetLocationsAsync));
        }

        public Task<BaseResponse<int>> SaveLocationAsync(StockLocationDto2 dto)
        {
            return StockResult2.RunAsync(async () =>
            {
                var errors = new List<string>();
                string code = (dto.Code ?? string.Empty).Trim().ToUpperInvariant();
                string name = (dto.Name ?? string.Empty).Trim();
                if (code.Length == 0) errors.Add("Code is required.");
                if (code.Length > 20) errors.Add("Code can be at most 20 characters.");
                if (name.Length == 0) errors.Add("Name is required.");
                if (name.Length > 100) errors.Add("Name can be at most 100 characters.");
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                await StockLedgerHelper2.EnsureDefaultLocationAsync(db);

                await using var tx = await db.Database.BeginTransactionAsync();

                if (await db.StockLocations2.AnyAsync(l => l.Code == code && l.Id != dto.Id))
                    return StockResult2.Fail<int>($"The code '{code}' is already used by another location.");

                bool isNew = dto.Id <= 0;
                StockLocation2? entity;
                if (isNew)
                {
                    entity = new StockLocation2 { CreatedBy = StockConstants2.SystemUserId, CreatedOn = DateTime.Now };
                }
                else
                {
                    entity = await db.StockLocations2.FirstOrDefaultAsync(l => l.Id == dto.Id);
                    if (entity == null) return StockResult2.Fail<int>("Location not found.");
                }

                bool wasDefault = entity.IsDefault;
                if (wasDefault && !dto.IsDefault)
                    return StockResult2.Fail<int>("Choose another location as the default instead of unchecking this one.");
                if (wasDefault && !dto.IsActive)
                    return StockResult2.Fail<int>("The default location cannot be deactivated.");

                if (!isNew && !dto.IsActive)
                {
                    if (await db.StockLocationBalances2.AnyAsync(b => b.LocationId == entity.Id && b.Quantity > 0))
                        return StockResult2.Fail<int>("This location still holds stock. Transfer or adjust it first.");
                    if (await db.StockTransfers2.AnyAsync(t => t.Status == StockConstants2.StatusInTransit
                            && (t.FromLocationId == entity.Id || t.ToLocationId == entity.Id)))
                        return StockResult2.Fail<int>("This location has transfers in transit.");
                }

                entity.Code = code;
                entity.Name = name;
                entity.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
                entity.IsActive = dto.IsActive;

                if (dto.IsDefault && !wasDefault)
                {
                    // The default location's quantity is derived, so switching the default is only safe
                    // while nothing is allocated to other locations or travelling between them.
                    if (await db.StockLocationBalances2.AnyAsync(b => b.Quantity > 0)
                        || await db.StockTransfers2.AnyAsync(t => t.Status == StockConstants2.StatusInTransit))
                        return StockResult2.Fail<int>("The default location cannot be changed while stock is allocated to other locations or in transit.");

                    var others = await db.StockLocations2.Where(l => l.IsDefault && l.Id != entity.Id).ToListAsync();
                    foreach (var other in others) other.IsDefault = false;
                    await db.SaveChangesAsync(); // clear the old default first (filtered unique index allows only one)
                }

                if (isNew) db.StockLocations2.Add(entity);
                entity.IsDefault = dto.IsDefault || wasDefault;

                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return StockResult2.Ok(entity.Id, "Location saved.");
            }, _logRepo, nameof(SaveLocationAsync), null, dto);
        }

        // ------------------------------------------------------------------ suppliers (existing Vendor table)

        public Task<BaseResponse<List<StockSupplierDto2>>> GetSuppliersAsync(bool includeInactive)
        {
            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var query = db.Vendor.AsNoTracking().AsQueryable();
                if (!includeInactive) query = query.Where(v => v.IsActive != false);

                var list = await query.OrderBy(v => v.Name).Select(v => new StockSupplierDto2
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

                return StockResult2.Ok(list);
            }, _logRepo, nameof(GetSuppliersAsync));
        }

        public Task<BaseResponse<int>> SaveSupplierAsync(StockSupplierDto2 dto)
        {
            return StockResult2.RunAsync(async () =>
            {
                string name = (dto.Name ?? string.Empty).Trim();
                if (name.Length == 0) return StockResult2.Fail<int>("Supplier name is required.");
                if (name.Length > 100) return StockResult2.Fail<int>("Supplier name can be at most 100 characters.");

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                if (await db.Vendor.AnyAsync(v => v.Name == name && v.Id != dto.Id))
                    return StockResult2.Fail<int>("A supplier with this name already exists.");

                Vendor? entity;
                if (dto.Id > 0)
                {
                    entity = await db.Vendor.FirstOrDefaultAsync(v => v.Id == dto.Id);
                    if (entity == null) return StockResult2.Fail<int>("Supplier not found.");
                }
                else
                {
                    entity = new Vendor { CreatedOn = DateTime.Now };
                    db.Vendor.Add(entity);
                }

                entity.Name = name;
                entity.Address = Clean(dto.Address, 250);
                entity.City = Clean(dto.City, 50);
                entity.State = Clean(dto.State, 50);
                entity.PostalCode = Clean(dto.PostalCode, 20);
                entity.Country = Clean(dto.Country, 50);
                entity.IsActive = dto.IsActive;

                await db.SaveChangesAsync();
                return StockResult2.Ok(entity.Id, "Supplier saved.");
            }, _logRepo, nameof(SaveSupplierAsync), null, dto);
        }

        private static string? Clean(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }

        // ------------------------------------------------------------------ reorder settings

        public Task<BaseResponse<PagedResult2<StockSettingDto2>>> GetSettingsAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var products = db.Products.AsNoTracking().Where(p => p.IsActive);
                if (filter.CategoryId.HasValue) products = products.Where(p => p.CategoryId == filter.CategoryId.Value);
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    string s = filter.Search.Trim();
                    products = products.Where(p => p.Name!.Contains(s) || p.ProductCode.Contains(s));
                }

                var productList = await products.Select(p => new { p.Id, p.ProductCode, p.Name }).ToListAsync();
                var settings = await db.ProductStockSettings2.AsNoTracking().ToDictionaryAsync(s => s.ProductId);
                var states = await StockLedgerHelper2.LoadStatesAsync(db);

                var rows = productList.Select(p =>
                {
                    settings.TryGetValue(p.Id, out var st);
                    states.TryGetValue(p.Id, out var state);
                    return new StockSettingDto2
                    {
                        ProductId = p.Id,
                        ProductCode = p.ProductCode,
                        ProductName = p.Name ?? string.Empty,
                        OnHand = state?.OnHand ?? 0,
                        ReorderLevel = st?.ReorderLevel ?? 0,
                        ReorderQuantity = st?.ReorderQuantity ?? 0,
                        MaxLevel = st?.MaxLevel,
                        PreferredVendorId = st?.PreferredVendorId
                    };
                });

                return StockResult2.Ok(StockPaging2.ToPage(rows, filter, nameof(StockSettingDto2.ProductName)));
            }, _logRepo, nameof(GetSettingsAsync));
        }

        public Task<BaseResponse<bool>> SaveSettingAsync(StockSettingDto2 dto, int? userId)
        {
            return StockResult2.RunAsync(async () =>
            {
                var errors = new List<string>();
                if (dto.ReorderLevel < 0) errors.Add("Reorder level cannot be negative.");
                if (dto.ReorderQuantity < 0) errors.Add("Reorder quantity cannot be negative.");
                if (dto.MaxLevel.HasValue && dto.MaxLevel.Value < dto.ReorderLevel)
                    errors.Add("Maximum level cannot be lower than the reorder level.");
                if (errors.Count > 0) return StockResult2.Fail<bool>(errors);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                if (!await db.Products.AnyAsync(p => p.Id == dto.ProductId && p.IsActive))
                    return StockResult2.Fail<bool>("Product not found.");

                if (dto.PreferredVendorId.HasValue
                    && !await db.Vendor.AnyAsync(v => v.Id == dto.PreferredVendorId.Value))
                    return StockResult2.Fail<bool>("Preferred supplier not found.");

                var entity = await db.ProductStockSettings2.FirstOrDefaultAsync(s => s.ProductId == dto.ProductId);
                if (entity == null)
                {
                    entity = new ProductStockSetting2 { ProductId = dto.ProductId };
                    db.ProductStockSettings2.Add(entity);
                }

                entity.ReorderLevel = dto.ReorderLevel;
                entity.ReorderQuantity = dto.ReorderQuantity;
                entity.MaxLevel = dto.MaxLevel;
                entity.PreferredVendorId = dto.PreferredVendorId;
                entity.ModifiedOn = DateTime.Now;
                entity.ModifiedBy = userId ?? StockConstants2.SystemUserId;

                await db.SaveChangesAsync();
                return StockResult2.Ok(true, "Reorder settings saved.");
            }, _logRepo, nameof(SaveSettingAsync), userId, dto);
        }
    }
}
