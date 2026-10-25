using Common.Stock2;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class StockIssueRepo2 : IStockIssueRepo2
    {
        private readonly InventoryDbContext _db;

        public StockIssueRepo2(InventoryDbContext db)
        {
            _db = db;
        }

        public async Task<int> CountIssuesWithPrefixAsync(string numberPrefix)
        {
            return await _db.StockIssues2.CountAsync(i => i.IssueNumber.StartsWith(numberPrefix));
        }

        public void AddIssue(StockIssue2 issue)
        {
            _db.StockIssues2.Add(issue);
        }

        public async Task<List<DocumentRowDto2>> GetIssueRowsAsync(StockQueryFilter2 filter)
        {
            var query = _db.StockIssues2.AsNoTracking().AsQueryable();
            if (filter.LocationId.HasValue) query = query.Where(i => i.LocationId == filter.LocationId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(i => i.Reason == filter.Status);
            if (filter.FromDate.HasValue) { var f = filter.FromDate.Value.Date; query = query.Where(i => i.IssueDate >= f); }
            if (filter.ToDate.HasValue) { var t = filter.ToDate.Value.Date.AddDays(1); query = query.Where(i => i.IssueDate < t); }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string s = filter.Search.Trim();
                query = query.Where(i => i.IssueNumber.Contains(s) || i.Reason.Contains(s));
            }

            return await query.Select(i => new DocumentRowDto2
            {
                Id = i.Id,
                Number = i.IssueNumber,
                Date = i.IssueDate,
                Status = i.Status,
                Party = i.Location!.Name,
                Reason = i.Reason,
                LineCount = i.Lines.Count,
                TotalQuantity = i.Lines.Sum(l => l.Quantity),
                TotalValue = i.Lines.Sum(l => l.Quantity * l.UnitCost),
                Remarks = i.Remarks ?? string.Empty
            }).ToListAsync();
        }

        public async Task<List<DocumentLineDto2>> GetIssueLinesAsync(int issueId)
        {
            return await _db.StockIssueLines2.AsNoTracking()
                .Where(l => l.IssueId == issueId)
                .OrderBy(l => l.Id)
                .Select(l => new DocumentLineDto2
                {
                    LineId = l.Id,
                    ProductId = l.ProductId,
                    ProductCode = l.Product!.ProductCode,
                    ProductName = l.Product!.Name ?? string.Empty,
                    Quantity = l.Quantity,
                    UnitCost = l.UnitCost
                }).ToListAsync();
        }
    }
}
