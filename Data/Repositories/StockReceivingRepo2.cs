using Common.Stock2;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class StockReceivingRepo2 : IStockReceivingRepo2
    {
        private readonly InventoryDbContext _db;

        public StockReceivingRepo2(InventoryDbContext db)
        {
            _db = db;
        }

        // ---------------------------------------------------------------- purchase orders

        public async Task<int> CountPurchaseOrdersWithPrefixAsync(string numberPrefix)
        {
            return await _db.PurchaseOrders2.CountAsync(p => p.PoNumber.StartsWith(numberPrefix));
        }

        public void AddPurchaseOrder(PurchaseOrder2 purchaseOrder)
        {
            _db.PurchaseOrders2.Add(purchaseOrder);
        }

        public async Task<PurchaseOrder2?> GetPurchaseOrderWithLinesAsync(int purchaseOrderId)
        {
            return await _db.PurchaseOrders2.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == purchaseOrderId);
        }

        public void MarkPurchaseOrderModified(PurchaseOrder2 purchaseOrder)
        {
            _db.Entry(purchaseOrder).Property(x => x.Status).IsModified = true;
        }

        public async Task<List<DocumentRowDto2>> GetPurchaseOrderRowsAsync(StockQueryFilter2 filter)
        {
            var query = _db.PurchaseOrders2.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(p => p.Status == filter.Status);
            if (filter.FromDate.HasValue) { var f = filter.FromDate.Value.Date; query = query.Where(p => p.PoDate >= f); }
            if (filter.ToDate.HasValue) { var t = filter.ToDate.Value.Date.AddDays(1); query = query.Where(p => p.PoDate < t); }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string s = filter.Search.Trim();
                query = query.Where(p => p.PoNumber.Contains(s) || (p.SupplierName != null && p.SupplierName.Contains(s)));
            }

            return await query.Select(p => new DocumentRowDto2
            {
                Id = p.Id,
                Number = p.PoNumber,
                Date = p.PoDate,
                Status = p.Status,
                Party = p.SupplierName ?? string.Empty,
                Reason = string.Empty,
                LineCount = p.Lines.Count,
                TotalQuantity = p.Lines.Sum(l => l.OrderedQuantity),
                TotalValue = p.Lines.Sum(l => l.OrderedQuantity * l.UnitCost),
                Remarks = p.Remarks ?? string.Empty
            }).ToListAsync();
        }

        public async Task<List<DocumentLineDto2>> GetPurchaseOrderLinesAsync(int purchaseOrderId)
        {
            return await _db.PurchaseOrderLines2.AsNoTracking()
                .Where(l => l.PurchaseOrderId == purchaseOrderId)
                .OrderBy(l => l.Id)
                .Select(l => new DocumentLineDto2
                {
                    LineId = l.Id,
                    ProductId = l.ProductId,
                    ProductCode = l.Product!.ProductCode,
                    ProductName = l.Product!.Name ?? string.Empty,
                    Quantity = l.OrderedQuantity,
                    ProcessedQuantity = l.ReceivedQuantity,
                    UnitCost = l.UnitCost
                }).ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetOnOrderQuantitiesAsync(ICollection<int> productIds)
        {
            return await _db.PurchaseOrderLines2.AsNoTracking()
                .Where(l => productIds.Contains(l.ProductId)
                    && (l.PurchaseOrder!.Status == StockConstants2.StatusOpen
                        || l.PurchaseOrder!.Status == StockConstants2.StatusPartiallyReceived))
                .GroupBy(l => l.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.OrderedQuantity - x.ReceivedQuantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Qty);
        }

        public async Task<int> CountOpenPurchaseOrdersAsync()
        {
            return await _db.PurchaseOrders2.CountAsync(p =>
                p.Status == StockConstants2.StatusOpen || p.Status == StockConstants2.StatusPartiallyReceived);
        }

        // ---------------------------------------------------------------- receipts

        public async Task<int> CountReceiptsWithPrefixAsync(string numberPrefix)
        {
            return await _db.StockReceipts2.CountAsync(r => r.ReceiptNumber.StartsWith(numberPrefix));
        }

        public void AddReceipt(StockReceipt2 receipt)
        {
            _db.StockReceipts2.Add(receipt);
        }

        public async Task<StockReceipt2?> GetReceiptForCancelAsync(int receiptId)
        {
            return await _db.StockReceipts2
                .Include(r => r.Lines).ThenInclude(l => l.PurchaseOrderLine)
                .Include(r => r.Location)
                .FirstOrDefaultAsync(r => r.Id == receiptId);
        }

        public async Task<List<DocumentRowDto2>> GetReceiptRowsAsync(StockQueryFilter2 filter)
        {
            var query = _db.StockReceipts2.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(r => r.Status == filter.Status);
            if (filter.LocationId.HasValue) query = query.Where(r => r.LocationId == filter.LocationId.Value);
            if (filter.FromDate.HasValue) { var f = filter.FromDate.Value.Date; query = query.Where(r => r.ReceiptDate >= f); }
            if (filter.ToDate.HasValue) { var t = filter.ToDate.Value.Date.AddDays(1); query = query.Where(r => r.ReceiptDate < t); }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string s = filter.Search.Trim();
                query = query.Where(r => r.ReceiptNumber.Contains(s)
                    || (r.SupplierName != null && r.SupplierName.Contains(s))
                    || (r.SupplierInvoiceNo != null && r.SupplierInvoiceNo.Contains(s)));
            }

            return await query.Select(r => new DocumentRowDto2
            {
                Id = r.Id,
                Number = r.ReceiptNumber,
                Date = r.ReceiptDate,
                Status = r.Status,
                Party = r.SupplierName ?? string.Empty,
                Reason = r.SupplierInvoiceNo ?? string.Empty,
                LineCount = r.Lines.Count,
                TotalQuantity = r.Lines.Sum(l => l.Quantity),
                TotalValue = r.Lines.Sum(l => l.Quantity * l.UnitCost),
                Remarks = r.Remarks ?? string.Empty
            }).ToListAsync();
        }

        public async Task<List<DocumentLineDto2>> GetReceiptLinesAsync(int receiptId)
        {
            return await _db.StockReceiptLines2.AsNoTracking()
                .Where(l => l.ReceiptId == receiptId)
                .OrderBy(l => l.Id)
                .Select(l => new DocumentLineDto2
                {
                    LineId = l.Id,
                    ProductId = l.ProductId,
                    ProductCode = l.Product!.ProductCode,
                    ProductName = l.Product!.Name ?? string.Empty,
                    Quantity = l.Quantity,
                    UnitCost = l.UnitCost,
                    BatchNumber = l.BatchNumber ?? string.Empty,
                    ExpiryDate = l.ExpiryDate
                }).ToListAsync();
        }
    }
}
