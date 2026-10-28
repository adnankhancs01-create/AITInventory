using Common.RetailPos2;
using Domain.Entities;
using Domain.IRepositories;
using Domain.RetailPos2;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class RetailPosReturnRepo2 : IRetailPosReturnRepo2
    {
        private readonly InventoryDbContext _db;

        public RetailPosReturnRepo2(InventoryDbContext db)
        {
            _db = db;
        }

        public Task<int> CountReturnsWithPrefixAsync(string numberPrefix)
        {
            return _db.Set<RetailPosReturn2>().CountAsync(r => r.ReturnNumber.StartsWith(numberPrefix));
        }

        public void AddReturn(RetailPosReturn2 saleReturn)
        {
            _db.Set<RetailPosReturn2>().Add(saleReturn);
        }

        public Task<RetailPosReturn2?> FindReturnByKeyAsync(Guid idempotencyKey)
        {
            return _db.Set<RetailPosReturn2>().AsNoTracking().FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey);
        }

        public Task<List<RetailPosSaleLine2>> GetSaleLineEntitiesAsync(int saleId)
        {
            return _db.Set<RetailPosSaleLine2>().AsNoTracking().Where(l => l.SaleId == saleId).OrderBy(l => l.Id).ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetReturnedQuantitiesAsync(int saleId)
        {
            var rows = await _db.Set<RetailPosReturnLine2>().AsNoTracking()
                .Where(l => l.Return!.SaleId == saleId)
                .GroupBy(l => l.SaleLineId)
                .Select(g => new { SaleLineId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToListAsync();

            return rows.ToDictionary(r => r.SaleLineId, r => r.Quantity);
        }

        public async Task<decimal> GetCreditedAmountAsync(int saleId)
        {
            return await _db.Set<RetailPosReturnLine2>().AsNoTracking()
                .Where(l => l.Return!.SaleId == saleId)
                .SumAsync(l => (decimal?)l.LineAmount) ?? 0m;
        }

        public async Task<Dictionary<int, RetailPosSaleStockLine2>> GetSaleStockLinesAsync(int saleId)
        {
            var rows = await _db.StockMovements2.AsNoTracking()
                .Where(m => m.DocumentType == RetailPosConstants2.StockDocumentType && m.DocumentId == saleId
                            && m.Quantity < 0 && m.LocationId != null)
                .Select(m => new { m.ProductId, LocationId = m.LocationId!.Value, m.Quantity, m.UnitCost })
                .ToListAsync();

            return rows.GroupBy(r => r.ProductId).ToDictionary(g => g.Key, g =>
            {
                var first = g.First();
                return new RetailPosSaleStockLine2
                {
                    ProductId = g.Key,
                    LocationId = first.LocationId,
                    Quantity = g.Sum(x => -x.Quantity),
                    UnitCost = first.UnitCost ?? 0m
                };
            });
        }

        public async Task<List<RetailPosReturnRowDto2>> GetReturnRowsAsync(RetailPosReturnFilterDto2 filter)
        {
            var query = _db.Set<RetailPosReturn2>().AsNoTracking().AsQueryable();

            if (filter.ClientId.HasValue) query = query.Where(r => r.ClientId == filter.ClientId.Value);
            if (filter.SaleId.HasValue) query = query.Where(r => r.SaleId == filter.SaleId.Value);
            if (filter.FromDate.HasValue) { var f = filter.FromDate.Value.Date; query = query.Where(r => r.ReturnDate >= f); }
            if (filter.ToDate.HasValue) { var t = filter.ToDate.Value.Date.AddDays(1); query = query.Where(r => r.ReturnDate < t); }

            var rows = await query.OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.Id)
                .Select(r => new
                {
                    r.Id, r.ReturnNumber, r.ReturnDate, r.SaleId,
                    InvoiceNumber = r.Sale!.InvoiceNumber,
                    ClientName = r.Client != null ? r.Client.Name : RetailPosConstants2.WalkInName,
                    r.CreditAmount, r.RefundAmount, r.RefundMethod, r.Reason, r.CreatedBy,
                    Lines = r.Lines.Select(l => new { l.ProductName, l.Quantity }).ToList()
                })
                .ToListAsync();

            var userIds = rows.Select(r => r.CreatedBy).Distinct().ToList();
            var users = await _db.UserMst.AsNoTracking()
                .Where(u => userIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.UserName, u.Email })
                .ToListAsync();
            var names = users.ToDictionary(u => u.UserId, u => u.UserName ?? u.Email ?? string.Empty);

            return rows.Select(r => new RetailPosReturnRowDto2
            {
                Id = r.Id,
                ReturnNumber = r.ReturnNumber,
                ReturnDate = r.ReturnDate,
                SaleId = r.SaleId,
                InvoiceNumber = r.InvoiceNumber,
                ClientName = r.ClientName,
                Items = string.Join(", ", r.Lines.Select(l => $"{l.Quantity} x {l.ProductName}")),
                CreditAmount = r.CreditAmount,
                RefundAmount = r.RefundAmount,
                RefundMethod = r.RefundMethod ?? string.Empty,
                Reason = r.Reason,
                CreatedBy = names.TryGetValue(r.CreatedBy, out var n) ? n : string.Empty
            }).ToList();
        }
    }
}
