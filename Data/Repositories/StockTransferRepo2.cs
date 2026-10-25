using Common.Stock2;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class StockTransferRepo2 : IStockTransferRepo2
    {
        private readonly InventoryDbContext _db;

        public StockTransferRepo2(InventoryDbContext db)
        {
            _db = db;
        }

        public async Task<int> CountTransfersWithPrefixAsync(string numberPrefix)
        {
            return await _db.StockTransfers2.CountAsync(t => t.TransferNumber.StartsWith(numberPrefix));
        }

        public void AddTransfer(StockTransfer2 transfer)
        {
            _db.StockTransfers2.Add(transfer);
        }

        public async Task<StockTransfer2?> GetTransferWithLinesAsync(int transferId)
        {
            return await _db.StockTransfers2
                .Include(t => t.Lines)
                .Include(t => t.FromLocation)
                .Include(t => t.ToLocation)
                .FirstOrDefaultAsync(t => t.Id == transferId);
        }

        public async Task<List<DocumentRowDto2>> GetTransferRowsAsync(StockQueryFilter2 filter)
        {
            var query = _db.StockTransfers2.AsNoTracking().AsQueryable();
            if (filter.LocationId.HasValue)
                query = query.Where(t => t.FromLocationId == filter.LocationId.Value || t.ToLocationId == filter.LocationId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(t => t.Status == filter.Status);
            if (filter.FromDate.HasValue) { var f = filter.FromDate.Value.Date; query = query.Where(t => t.TransferDate >= f); }
            if (filter.ToDate.HasValue) { var e = filter.ToDate.Value.Date.AddDays(1); query = query.Where(t => t.TransferDate < e); }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string s = filter.Search.Trim();
                query = query.Where(t => t.TransferNumber.Contains(s)
                    || t.FromLocation!.Name.Contains(s) || t.ToLocation!.Name.Contains(s));
            }

            return await query.Select(t => new DocumentRowDto2
            {
                Id = t.Id,
                Number = t.TransferNumber,
                Date = t.TransferDate,
                Status = t.Status,
                Party = t.FromLocation!.Name + " -> " + t.ToLocation!.Name,
                LineCount = t.Lines.Count,
                TotalQuantity = t.Lines.Sum(l => l.Quantity),
                Remarks = t.Remarks ?? string.Empty
            }).ToListAsync();
        }

        public async Task<List<DocumentLineDto2>> GetTransferLinesAsync(int transferId)
        {
            return await _db.StockTransferLines2.AsNoTracking()
                .Where(l => l.TransferId == transferId)
                .OrderBy(l => l.Id)
                .Select(l => new DocumentLineDto2
                {
                    LineId = l.Id,
                    ProductId = l.ProductId,
                    ProductCode = l.Product!.ProductCode,
                    ProductName = l.Product!.Name ?? string.Empty,
                    Quantity = l.Quantity
                }).ToListAsync();
        }

        public async Task<int> CountInTransitTransfersAsync()
        {
            return await _db.StockTransfers2.CountAsync(t => t.Status == StockConstants2.StatusInTransit);
        }
    }
}
