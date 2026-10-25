using Common.Stock2;
using Domain.Entities;
using Domain.IRepositories;
using Domain.Stock2;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class StockLedgerRepo2 : IStockLedgerRepo2
    {
        private readonly InventoryDbContext _db;

        public StockLedgerRepo2(InventoryDbContext db)
        {
            _db = db;
        }

        public async Task<StockLocation2> EnsureDefaultLocationAsync()
        {
            var def = await _db.StockLocations2.FirstOrDefaultAsync(l => l.IsDefault);
            if (def != null) return def;

            var anyActive = await _db.StockLocations2.OrderBy(l => l.Id).FirstOrDefaultAsync(l => l.IsActive);
            if (anyActive != null)
            {
                anyActive.IsDefault = true;
                await StockDbSave2.SaveAsync(_db);
                return anyActive;
            }

            def = new StockLocation2
            {
                Code = StockConstants2.DefaultLocationCode,
                Name = StockConstants2.DefaultLocationName,
                IsDefault = true,
                IsActive = true,
                CreatedBy = StockConstants2.SystemUserId
            };
            _db.StockLocations2.Add(def);
            await StockDbSave2.SaveAsync(_db);
            return def;
        }

        public async Task<StockLocation2?> GetLocationAsync(int locationId, bool activeOnly)
        {
            return await _db.StockLocations2.FirstOrDefaultAsync(l => l.Id == locationId && (!activeOnly || l.IsActive));
        }

        public async Task<List<StockLedgerRow2>> GetActiveLedgerAsync(ICollection<int>? productIds = null)
        {
            var query = _db.Stock.AsNoTracking()
                .Where(s => s.IsActive == true && s.ProductId != null);

            if (productIds != null)
                query = query.Where(s => productIds.Contains(s.ProductId!.Value));

            return await query.Select(s => new StockLedgerRow2
            {
                Id = s.Id,
                ProductId = s.ProductId!.Value,
                Quantity = s.Quantity ?? 0,
                TotalPurchasePrice = s.TotalPurchasePrice,
                CreatedOn = s.CreatedOn ?? DateTime.MinValue
            }).ToListAsync();
        }

        public async Task<Dictionary<int, StockProductState2>> GetStatesAsync(ICollection<int>? productIds = null)
        {
            var rows = await GetActiveLedgerAsync(productIds);
            return StockLayerCalculator2.Build(rows);
        }

        public async Task<Dictionary<int, int>> GetBalanceTotalsAsync(ICollection<int>? productIds = null)
        {
            var query = _db.StockLocationBalances2.AsNoTracking().AsQueryable();
            if (productIds != null)
                query = query.Where(b => productIds.Contains(b.ProductId));

            return await query.GroupBy(b => b.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Qty);
        }

        public async Task<Dictionary<int, int>> GetLocationBalancesAsync(int locationId)
        {
            return await _db.StockLocationBalances2.AsNoTracking()
                .Where(b => b.LocationId == locationId)
                .ToDictionaryAsync(b => b.ProductId, b => b.Quantity);
        }

        public async Task<Dictionary<int, int>> GetInTransitAsync(ICollection<int>? productIds = null)
        {
            var query = _db.StockTransferLines2.AsNoTracking()
                .Where(l => l.Transfer!.Status == StockConstants2.StatusInTransit);

            if (productIds != null)
                query = query.Where(l => productIds.Contains(l.ProductId));

            return await query.GroupBy(l => l.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Qty);
        }

        public async Task<int> GetQuantityAtLocationAsync(int productId, StockLocation2 location)
        {
            if (!location.IsDefault)
            {
                return await _db.StockLocationBalances2
                    .Where(b => b.ProductId == productId && b.LocationId == location.Id)
                    .Select(b => b.Quantity).FirstOrDefaultAsync();
            }

            var ids = new List<int> { productId };
            var onHandRows = await GetActiveLedgerAsync(ids);
            int onHand = onHandRows.Sum(r => r.Quantity);
            int balances = StockLedgerRules2.Get(await GetBalanceTotalsAsync(ids), productId);
            int transit = StockLedgerRules2.Get(await GetInTransitAsync(ids), productId);
            return StockLedgerRules2.QuantityAtLocation(onHand, balances, transit, true, 0);
        }

        public async Task ChangeBalanceAsync(int productId, StockLocation2 location, int delta)
        {
            if (location.IsDefault || delta == 0) return;

            // Check rows added earlier in the same unit of work first (a receipt may list one product twice).
            var row = _db.StockLocationBalances2.Local
                .FirstOrDefault(b => b.ProductId == productId && b.LocationId == location.Id)
                ?? await _db.StockLocationBalances2
                    .FirstOrDefaultAsync(b => b.ProductId == productId && b.LocationId == location.Id);

            if (row == null)
            {
                row = new StockLocationBalance2 { ProductId = productId, LocationId = location.Id, Quantity = 0 };
                _db.StockLocationBalances2.Add(row);
            }

            int result = row.Quantity + delta;
            if (result < 0)
                throw new InvalidOperationException($"Location '{location.Name}' cannot go below zero for product {productId}.");

            row.Quantity = result;
            row.ModifiedOn = DateTime.Now;
        }

        public void AddLedgerRow(Stock row)
        {
            _db.Stock.Add(row);
        }

        public void AddMovement(StockMovement2 movement)
        {
            _db.StockMovements2.Add(movement);
        }

        public void AddBatch(StockBatch2 batch)
        {
            _db.StockBatches2.Add(batch);
        }

        public async Task<List<Stock>> GetLedgerRowsAsync(ICollection<int> ledgerIds)
        {
            return await _db.Stock.Where(v => ledgerIds.Contains(v.Id)).ToListAsync();
        }

        public async Task<Dictionary<int, string>> GetActiveProductNamesAsync(ICollection<int> productIds)
        {
            return await _db.Products.AsNoTracking()
                .Where(p => p.IsActive && productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name ?? p.ProductCode);
        }

        public async Task<Dictionary<int, string>> GetProductNamesAsync(ICollection<int> productIds)
        {
            return await _db.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name ?? p.ProductCode);
        }
    }
}
