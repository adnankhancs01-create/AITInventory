using Common.RetailPos2;
using Domain.Entities;
using Domain.IRepositories;
using Domain.RetailPos2;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class RetailPosSaleRepo2 : IRetailPosSaleRepo2
    {
        private readonly InventoryDbContext _db;

        public RetailPosSaleRepo2(InventoryDbContext db)
        {
            _db = db;
        }

        public Task<int> CountSalesWithPrefixAsync(string numberPrefix)
        {
            return _db.Set<RetailPosSale2>().CountAsync(s => s.InvoiceNumber.StartsWith(numberPrefix));
        }

        public void AddSale(RetailPosSale2 sale)
        {
            _db.Set<RetailPosSale2>().Add(sale);
        }

        public Task<RetailPosSale2?> FindSaleByKeyAsync(Guid idempotencyKey)
        {
            return _db.Set<RetailPosSale2>().AsNoTracking().FirstOrDefaultAsync(s => s.IdempotencyKey == idempotencyKey);
        }

        public Task<RetailPosSale2?> GetSaleAsync(int saleId)
        {
            return _db.Set<RetailPosSale2>().FirstOrDefaultAsync(s => s.Id == saleId);
        }

        public async Task<RetailPosSaleBalance2?> GetSaleBalanceAsync(int saleId)
        {
            var sale = await _db.Set<RetailPosSale2>().AsNoTracking()
                .Where(s => s.Id == saleId)
                .Select(s => new { s.Id, s.ClientId, s.InvoiceNumber, s.TotalAmount })
                .FirstOrDefaultAsync();
            if (sale == null) return null;

            decimal paid = await _db.Set<RetailPosPayment2>().Where(p => p.SaleId == saleId && !p.IsReversed)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;
            decimal adjustments = await _db.Set<RetailPosAdjustment2>().Where(a => a.SaleId == saleId)
                .SumAsync(a => (decimal?)a.Amount) ?? 0m;

            return new RetailPosSaleBalance2
            {
                SaleId = sale.Id,
                ClientId = sale.ClientId,
                InvoiceNumber = sale.InvoiceNumber,
                TotalAmount = sale.TotalAmount,
                PaidAmount = paid,
                AdjustmentAmount = adjustments
            };
        }

        public async Task<List<RetailPosSaleRowDto2>> GetSaleRowsAsync(RetailPosSaleFilterDto2 filter)
        {
            var query = _db.Set<RetailPosSale2>().AsNoTracking().AsQueryable();

            if (filter.ClientId.HasValue) query = query.Where(s => s.ClientId == filter.ClientId.Value);
            if (filter.FromDate.HasValue) { var f = filter.FromDate.Value.Date; query = query.Where(s => s.SaleDate >= f); }
            if (filter.ToDate.HasValue) { var t = filter.ToDate.Value.Date.AddDays(1); query = query.Where(s => s.SaleDate < t); }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string term = filter.Search.Trim();
                query = query.Where(s => s.InvoiceNumber.Contains(term)
                    || (s.Client != null && s.Client.Name.Contains(term)));
            }

            return await ProjectRows(query).OrderByDescending(r => r.SaleDate).ThenByDescending(r => r.Id).ToListAsync();
        }

        public Task<RetailPosSaleRowDto2?> GetSaleRowAsync(int saleId)
        {
            var query = _db.Set<RetailPosSale2>().AsNoTracking().Where(s => s.Id == saleId);
            return ProjectRows(query).FirstOrDefaultAsync();
        }

        public Task<List<RetailPosSaleLineRowDto2>> GetSaleLinesAsync(int saleId)
        {
            return _db.Set<RetailPosSaleLine2>().AsNoTracking()
                .Where(l => l.SaleId == saleId)
                .OrderBy(l => l.Id)
                .Select(l => new RetailPosSaleLineRowDto2
                {
                    Id = l.Id,
                    ProductCode = l.ProductCode,
                    ProductName = l.ProductName,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    LineTotal = l.LineTotal
                }).ToListAsync();
        }

        public async Task<List<RetailPosProductLookupDto2>> GetProductLookupAsync()
        {
            var products = await _db.Products.AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.ProductCode, p.Name })
                .ToListAsync();

            var priceRows = await _db.Pricing.AsNoTracking()
                .Where(x => x.ProductCode != null && x.UnitPrice != null)
                .Select(x => new { x.Id, x.ProductCode, x.UnitPrice })
                .ToListAsync();
            var priceByCode = priceRows.GroupBy(x => x.ProductCode!)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First().UnitPrice ?? 0m);

            return products.Select(p => new RetailPosProductLookupDto2
            {
                ProductId = p.Id,
                ProductCode = p.ProductCode,
                ProductName = p.Name ?? string.Empty,
                UnitPrice = priceByCode.TryGetValue(p.ProductCode, out var price) ? price : 0m
            }).ToList();
        }

        public async Task<Dictionary<int, RetailPosProductInfo2>> GetActiveProductsAsync(ICollection<int> productIds)
        {
            var list = await _db.Products.AsNoTracking()
                .Where(p => p.IsActive && productIds.Contains(p.Id))
                .Select(p => new RetailPosProductInfo2 { Id = p.Id, Code = p.ProductCode, Name = p.Name ?? p.ProductCode })
                .ToListAsync();

            return list.ToDictionary(p => p.Id);
        }

        private IQueryable<RetailPosSaleRowDto2> ProjectRows(IQueryable<RetailPosSale2> query)
        {
            return query.Select(s => new RetailPosSaleRowDto2
            {
                Id = s.Id,
                InvoiceNumber = s.InvoiceNumber,
                SaleDate = s.SaleDate,
                ClientId = s.ClientId,
                ClientName = s.Client != null ? s.Client.Name : RetailPosConstants2.WalkInName,
                SubTotal = s.SubTotal,
                DiscountAmount = s.DiscountAmount,
                TotalAmount = s.TotalAmount,
                PaymentType = s.PaymentType,
                DueDate = s.DueDate,
                Remarks = s.Remarks ?? string.Empty,
                PaidAmount = _db.Set<RetailPosPayment2>().Where(p => p.SaleId == s.Id && !p.IsReversed).Sum(p => (decimal?)p.Amount) ?? 0m,
                AdjustmentAmount = _db.Set<RetailPosAdjustment2>().Where(a => a.SaleId == s.Id).Sum(a => (decimal?)a.Amount) ?? 0m
            });
        }
    }
}
