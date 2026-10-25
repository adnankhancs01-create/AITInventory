using Common.Stock2;
using Domain.Entities;
using Domain.IRepositories;
using Domain.Stock2;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class StockReportRepo2 : IStockReportRepo2
    {
        private readonly InventoryDbContext _db;

        public StockReportRepo2(InventoryDbContext db)
        {
            _db = db;
        }

        public async Task<List<StockProductInfo2>> GetProductsAsync(StockQueryFilter2 filter, bool includeInactive)
        {
            var query = _db.Products.AsNoTracking().AsQueryable();
            if (!includeInactive) query = query.Where(p => p.IsActive);
            if (filter.ProductId.HasValue) query = query.Where(p => p.Id == filter.ProductId.Value);
            if (filter.CategoryId.HasValue) query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string s = filter.Search.Trim();
                query = query.Where(p => p.Name!.Contains(s) || p.ProductCode.Contains(s));
            }

            return await query.Select(p => new StockProductInfo2
            {
                Id = p.Id,
                Code = p.ProductCode,
                Name = p.Name ?? string.Empty,
                CategoryId = p.CategoryId,
                Category = p.Category.Name
            }).ToListAsync();
        }

        public async Task<Dictionary<string, decimal?>> GetLatestSellPricesAsync()
        {
            var priceRows = await _db.Pricing.AsNoTracking()
                .Where(x => x.ProductCode != null && x.UnitPrice != null)
                .Select(x => new { x.Id, x.ProductCode, x.UnitPrice })
                .ToListAsync();

            return priceRows.GroupBy(x => x.ProductCode!)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First().UnitPrice);
        }

        public async Task<Dictionary<int, DateTime?>> GetLastMovementDatesAsync()
        {
            var lastMoves = await _db.Stock.AsNoTracking()
                .Where(s => s.IsActive == true && s.ProductId != null)
                .GroupBy(s => s.ProductId)
                .Select(g => new { ProductId = g.Key, Last = g.Max(x => x.CreatedOn) })
                .ToListAsync();

            return lastMoves.Where(x => x.ProductId.HasValue)
                .ToDictionary(x => x.ProductId!.Value, x => x.Last);
        }

        public async Task<StockLocation2?> FindLocationAsync(int locationId)
        {
            return await _db.StockLocations2.AsNoTracking().FirstOrDefaultAsync(l => l.Id == locationId);
        }

        public async Task<Dictionary<int, string>> GetLocationNamesAsync()
        {
            return await _db.StockLocations2.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Name);
        }

        public async Task<List<StockLedgerMovementRow2>> GetLedgerMovementRowsAsync(ICollection<int>? productIds, DateTime? fromDate, DateTime? toExclusive)
        {
            var ledgerQuery = _db.Stock.AsNoTracking().Where(s => s.ProductId != null);
            if (productIds != null) ledgerQuery = ledgerQuery.Where(s => productIds.Contains(s.ProductId!.Value));
            if (fromDate.HasValue) ledgerQuery = ledgerQuery.Where(s => s.CreatedOn >= fromDate.Value);
            if (toExclusive.HasValue) ledgerQuery = ledgerQuery.Where(s => s.CreatedOn < toExclusive.Value);

            return await ledgerQuery.Select(s => new StockLedgerMovementRow2
            {
                Id = s.Id,
                ProductId = s.ProductId!.Value,
                Quantity = s.Quantity ?? 0,
                TotalPurchasePrice = s.TotalPurchasePrice,
                StockNumber = s.StockNumber,
                CreatedOn = s.CreatedOn ?? DateTime.MinValue,
                IsActive = s.IsActive,
                TransactionType = s.TransactionDetails!.TransMst!.TransactionType,
                TransactionNumber = s.TransactionDetails!.TransMst!.TransactionNumber
            }).ToListAsync();
        }

        public async Task<List<StockMovement2>> GetMovementsForLedgerRowsAsync(ICollection<int> ledgerIds)
        {
            if (ledgerIds.Count == 0) return new List<StockMovement2>();

            return await _db.StockMovements2.AsNoTracking()
                .Where(m => m.VendorStockId != null && ledgerIds.Contains(m.VendorStockId.Value))
                .ToListAsync();
        }

        public async Task<List<StockMovement2>> GetLocationOnlyMovementsAsync(ICollection<int>? productIds, DateTime? fromDate, DateTime? toExclusive, int? locationId, string? movementType)
        {
            var query = _db.StockMovements2.AsNoTracking().Where(m => m.VendorStockId == null);
            if (productIds != null) query = query.Where(m => productIds.Contains(m.ProductId));
            if (fromDate.HasValue) query = query.Where(m => m.CreatedOn >= fromDate.Value);
            if (toExclusive.HasValue) query = query.Where(m => m.CreatedOn < toExclusive.Value);
            if (locationId.HasValue) query = query.Where(m => m.LocationId == locationId.Value);
            if (!string.IsNullOrWhiteSpace(movementType)) query = query.Where(m => m.MovementType == movementType);

            return await query.ToListAsync();
        }

        public async Task<List<StockBatchInfo2>> GetBatchesWithExpiryAsync(ICollection<int>? productIds)
        {
            var batchQuery = _db.StockBatches2.AsNoTracking().Where(b => b.ExpiryDate != null);
            if (productIds != null) batchQuery = batchQuery.Where(b => productIds.Contains(b.ProductId));

            return await batchQuery.Select(b => new StockBatchInfo2
            {
                ProductId = b.ProductId,
                VendorStockId = b.VendorStockId,
                BatchNumber = b.BatchNumber,
                ExpiryDate = b.ExpiryDate!.Value
            }).ToListAsync();
        }
    }
}
