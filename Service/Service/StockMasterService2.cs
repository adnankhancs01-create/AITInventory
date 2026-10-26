using Common;
using Common.Stock2;
using Domain.Entities;
using Domain.IRepositories;
using Domain.Users2;
using Service.IService;

namespace Service.Service
{
    public class StockMasterService2 : IStockMasterService2
    {
        private readonly IStockUnitOfWorkFactory2 _uowFactory;
        private readonly ILogRepository _logRepo;
        private readonly ICurrentUser2 _currentUser;

        public StockMasterService2(IStockUnitOfWorkFactory2 uowFactory, ILogRepository logRepo, ICurrentUser2 currentUser)
        {
            _uowFactory = uowFactory;
            _logRepo = logRepo;
            _currentUser = currentUser;
        }

        // ------------------------------------------------------------------ locations

        public Task<BaseResponse<List<StockLocationDto2>>> GetLocationsAsync(bool includeInactive)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();
                await uow.Ledger.EnsureDefaultLocationAsync();
                var list = await uow.Master.GetLocationDtosAsync(includeInactive);
                return StockResult2.Ok(list);
            }, _logRepo, nameof(GetLocationsAsync));
        }

        public Task<BaseResponse<int>> SaveLocationAsync(StockLocationDto2 dto)
        {
            return StockResult2.RunAsync(async () =>
            {
                var denied = StockAccess2.Check<int>(_currentUser, ScreenCatalog2.StockLocations);
                if (denied != null) return denied;

                var errors = new List<string>();
                string code = (dto.Code ?? string.Empty).Trim().ToUpperInvariant();
                string name = (dto.Name ?? string.Empty).Trim();
                if (code.Length == 0) errors.Add("Code is required.");
                if (code.Length > 20) errors.Add("Code can be at most 20 characters.");
                if (name.Length == 0) errors.Add("Name is required.");
                if (name.Length > 100) errors.Add("Name can be at most 100 characters.");
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                await using var uow = _uowFactory.Create();
                await uow.Ledger.EnsureDefaultLocationAsync();
                await uow.BeginTransactionAsync();

                if (await uow.Master.LocationCodeExistsAsync(code, dto.Id))
                    return StockResult2.Fail<int>($"The code '{code}' is already used by another location.");

                bool isNew = dto.Id <= 0;
                StockLocation2? entity;
                if (isNew)
                {
                    entity = new StockLocation2 { CreatedBy = StockAccess2.ActingUserId(_currentUser, null), CreatedOn = DateTime.Now };
                }
                else
                {
                    entity = await uow.Ledger.GetLocationAsync(dto.Id, false);
                    if (entity == null) return StockResult2.Fail<int>("Location not found.");
                }

                bool wasDefault = entity.IsDefault;
                if (wasDefault && !dto.IsDefault)
                    return StockResult2.Fail<int>("Choose another location as the default instead of unchecking this one.");
                if (wasDefault && !dto.IsActive)
                    return StockResult2.Fail<int>("The default location cannot be deactivated.");

                if (!isNew && !dto.IsActive)
                {
                    if (await uow.Master.LocationHoldsStockAsync(entity.Id))
                        return StockResult2.Fail<int>("This location still holds stock. Transfer or adjust it first.");
                    if (await uow.Master.LocationHasTransfersInTransitAsync(entity.Id))
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
                    if (await uow.Master.AnyStockAllocatedToOtherLocationsAsync()
                        || await uow.Master.AnyTransferInTransitAsync())
                        return StockResult2.Fail<int>("The default location cannot be changed while stock is allocated to other locations or in transit.");

                    var others = await uow.Master.GetOtherDefaultLocationsAsync(entity.Id);
                    foreach (var other in others) other.IsDefault = false;
                    await uow.SaveChangesAsync(); // clear the old default first (filtered unique index allows only one)
                }

                if (isNew) uow.Master.AddLocation(entity);
                entity.IsDefault = dto.IsDefault || wasDefault;

                await uow.SaveChangesAsync();
                await uow.CommitAsync();
                return StockResult2.Ok(entity.Id, "Location saved.");
            }, _logRepo, nameof(SaveLocationAsync), null, dto);
        }

        // ------------------------------------------------------------------ suppliers (existing Vendor table)

        public Task<BaseResponse<List<StockSupplierDto2>>> GetSuppliersAsync(bool includeInactive)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();
                var list = await uow.Master.GetSupplierDtosAsync(includeInactive);
                return StockResult2.Ok(list);
            }, _logRepo, nameof(GetSuppliersAsync));
        }

        public Task<BaseResponse<int>> SaveSupplierAsync(StockSupplierDto2 dto)
        {
            return StockResult2.RunAsync(async () =>
            {
                var denied = StockAccess2.Check<int>(_currentUser, ScreenCatalog2.StockSuppliers);
                if (denied != null) return denied;

                string name = (dto.Name ?? string.Empty).Trim();
                if (name.Length == 0) return StockResult2.Fail<int>("Supplier name is required.");
                if (name.Length > 100) return StockResult2.Fail<int>("Supplier name can be at most 100 characters.");

                await using var uow = _uowFactory.Create();

                if (await uow.Master.SupplierNameExistsAsync(name, dto.Id))
                    return StockResult2.Fail<int>("A supplier with this name already exists.");

                Vendor? entity;
                if (dto.Id > 0)
                {
                    entity = await uow.Master.GetSupplierAsync(dto.Id);
                    if (entity == null) return StockResult2.Fail<int>("Supplier not found.");
                }
                else
                {
                    entity = new Vendor { CreatedOn = DateTime.Now };
                    uow.Master.AddSupplier(entity);
                }

                entity.Name = name;
                entity.Address = Clean(dto.Address, 250);
                entity.City = Clean(dto.City, 50);
                entity.State = Clean(dto.State, 50);
                entity.PostalCode = Clean(dto.PostalCode, 20);
                entity.Country = Clean(dto.Country, 50);
                entity.IsActive = dto.IsActive;

                await uow.SaveChangesAsync();
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
                await using var uow = _uowFactory.Create();

                var productList = await uow.Reports.GetProductsAsync(filter, false);
                var settings = await uow.Master.GetSettingsByProductAsync();
                var states = await uow.Ledger.GetStatesAsync();

                var rows = productList.Select(p =>
                {
                    settings.TryGetValue(p.Id, out var st);
                    states.TryGetValue(p.Id, out var state);
                    return new StockSettingDto2
                    {
                        ProductId = p.Id,
                        ProductCode = p.Code,
                        ProductName = p.Name,
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
                var denied = StockAccess2.Check<bool>(_currentUser, ScreenCatalog2.StockReorder);
                if (denied != null) return denied;

                var errors = new List<string>();
                if (dto.ReorderLevel < 0) errors.Add("Reorder level cannot be negative.");
                if (dto.ReorderQuantity < 0) errors.Add("Reorder quantity cannot be negative.");
                if (dto.MaxLevel.HasValue && dto.MaxLevel.Value < dto.ReorderLevel)
                    errors.Add("Maximum level cannot be lower than the reorder level.");
                if (errors.Count > 0) return StockResult2.Fail<bool>(errors);

                await using var uow = _uowFactory.Create();

                if (!await uow.Master.ActiveProductExistsAsync(dto.ProductId))
                    return StockResult2.Fail<bool>("Product not found.");

                if (dto.PreferredVendorId.HasValue
                    && await uow.Master.FindSupplierAsync(dto.PreferredVendorId.Value) == null)
                    return StockResult2.Fail<bool>("Preferred supplier not found.");

                var entity = await uow.Master.GetSettingAsync(dto.ProductId);
                if (entity == null)
                {
                    entity = new ProductStockSetting2 { ProductId = dto.ProductId };
                    uow.Master.AddSetting(entity);
                }

                entity.ReorderLevel = dto.ReorderLevel;
                entity.ReorderQuantity = dto.ReorderQuantity;
                entity.MaxLevel = dto.MaxLevel;
                entity.PreferredVendorId = dto.PreferredVendorId;
                entity.ModifiedOn = DateTime.Now;
                entity.ModifiedBy = StockAccess2.ActingUserId(_currentUser, userId);

                await uow.SaveChangesAsync();
                return StockResult2.Ok(true, "Reorder settings saved.");
            }, _logRepo, nameof(SaveSettingAsync), userId, dto);
        }
    }
}
