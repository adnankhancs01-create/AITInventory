using Common.Stock2;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class StockAdjustmentRepo2 : IStockAdjustmentRepo2
    {
        private readonly InventoryDbContext _db;

        public StockAdjustmentRepo2(InventoryDbContext db)
        {
            _db = db;
        }

        public async Task<int> CountAdjustmentsWithPrefixAsync(string numberPrefix)
        {
            return await _db.StockAdjustments2.CountAsync(a => a.AdjustmentNumber.StartsWith(numberPrefix));
        }

        public void AddAdjustment(StockAdjustment2 adjustment)
        {
            _db.StockAdjustments2.Add(adjustment);
        }

        public async Task<List<DocumentRowDto2>> GetAdjustmentRowsAsync(StockQueryFilter2 filter)
        {
            var query = _db.StockAdjustments2.AsNoTracking().AsQueryable();
            if (filter.LocationId.HasValue) query = query.Where(a => a.LocationId == filter.LocationId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(a => a.Reason == filter.Status);
            if (filter.FromDate.HasValue) { var f = filter.FromDate.Value.Date; query = query.Where(a => a.AdjustmentDate >= f); }
            if (filter.ToDate.HasValue) { var e = filter.ToDate.Value.Date.AddDays(1); query = query.Where(a => a.AdjustmentDate < e); }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string s = filter.Search.Trim();
                query = query.Where(a => a.AdjustmentNumber.Contains(s) || a.Reason.Contains(s));
            }

            return await query.Select(a => new DocumentRowDto2
            {
                Id = a.Id,
                Number = a.AdjustmentNumber,
                Date = a.AdjustmentDate,
                Status = a.Status,
                Party = a.Location!.Name,
                Reason = a.Reason,
                LineCount = a.Lines.Count,
                TotalQuantity = a.Lines.Sum(l => l.Difference),
                TotalValue = a.Lines.Sum(l => l.Difference * l.UnitCost),
                Remarks = a.Remarks ?? string.Empty
            }).ToListAsync();
        }

        public async Task<List<DocumentLineDto2>> GetAdjustmentLinesAsync(int adjustmentId)
        {
            return await _db.StockAdjustmentLines2.AsNoTracking()
                .Where(l => l.AdjustmentId == adjustmentId)
                .OrderBy(l => l.Id)
                .Select(l => new DocumentLineDto2
                {
                    LineId = l.Id,
                    ProductId = l.ProductId,
                    ProductCode = l.Product!.ProductCode,
                    ProductName = l.Product!.Name ?? string.Empty,
                    Quantity = l.CountedQuantity,
                    ProcessedQuantity = l.SystemQuantity,
                    UnitCost = l.UnitCost,
                    Note = l.Remarks ?? string.Empty
                }).ToListAsync();
        }
    }
}
