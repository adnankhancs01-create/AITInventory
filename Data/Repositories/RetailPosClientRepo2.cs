using Common.RetailPos2;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class RetailPosClientRepo2 : IRetailPosClientRepo2
    {
        private readonly InventoryDbContext _db;

        public RetailPosClientRepo2(InventoryDbContext db)
        {
            _db = db;
        }

        public Task<RetailPosClient2?> GetClientAsync(int clientId)
        {
            return _db.Set<RetailPosClient2>().FirstOrDefaultAsync(c => c.Id == clientId);
        }

        public Task<RetailPosClient2?> FindClientAsync(int clientId)
        {
            return _db.Set<RetailPosClient2>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId);
        }

        public Task<bool> ClientExistsAsync(string name, string? phone, int excludeClientId)
        {
            return _db.Set<RetailPosClient2>().AnyAsync(c => c.Id != excludeClientId
                && c.Name == name
                && (c.Phone ?? "") == (phone ?? ""));
        }

        public void AddClient(RetailPosClient2 client)
        {
            _db.Set<RetailPosClient2>().Add(client);
        }

        public async Task<List<RetailPosClientRowDto2>> GetClientRowsAsync(RetailPosClientFilterDto2 filter)
        {
            var query = _db.Set<RetailPosClient2>().AsNoTracking().AsQueryable();

            if (filter.IsActive.HasValue) query = query.Where(c => c.IsActive == filter.IsActive.Value);
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string s = filter.Search.Trim();
                query = query.Where(c => c.Name.Contains(s)
                    || (c.Phone != null && c.Phone.Contains(s))
                    || (c.Address != null && c.Address.Contains(s)));
            }

            var rows = await ProjectRows(query).OrderBy(r => r.Name).ToListAsync();
            foreach (var row in rows) SetBalance(row);

            if (filter.OnlyWithBalance) rows = rows.Where(r => r.Balance > 0m).ToList();
            return rows;
        }

        public async Task<RetailPosClientRowDto2?> GetClientRowAsync(int clientId)
        {
            var query = _db.Set<RetailPosClient2>().AsNoTracking().Where(c => c.Id == clientId);
            var row = await ProjectRows(query).FirstOrDefaultAsync();
            if (row != null) SetBalance(row);
            return row;
        }

        public async Task<decimal> GetBalanceAsync(int clientId)
        {
            decimal opening = await _db.Set<RetailPosClient2>().Where(c => c.Id == clientId)
                .Select(c => c.OpeningBalance).FirstOrDefaultAsync();
            decimal sales = await _db.Set<RetailPosSale2>().Where(s => s.ClientId == clientId)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;
            decimal adjustments = await _db.Set<RetailPosAdjustment2>().Where(a => a.ClientId == clientId)
                .SumAsync(a => (decimal?)a.Amount) ?? 0m;
            decimal paid = await _db.Set<RetailPosPayment2>().Where(p => p.ClientId == clientId && !p.IsReversed)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            return opening + sales + adjustments - paid;
        }

        private IQueryable<RetailPosClientRowDto2> ProjectRows(IQueryable<RetailPosClient2> query)
        {
            return query.Select(c => new RetailPosClientRowDto2
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone ?? string.Empty,
                Address = c.Address ?? string.Empty,
                IsActive = c.IsActive,
                CreatedOn = c.CreatedOn,
                OpeningBalance = c.OpeningBalance,
                CreditLimit = c.CreditLimit,
                TotalSales = _db.Set<RetailPosSale2>().Where(s => s.ClientId == c.Id).Sum(s => (decimal?)s.TotalAmount) ?? 0m,
                TotalPaid = _db.Set<RetailPosPayment2>().Where(p => p.ClientId == c.Id && !p.IsReversed).Sum(p => (decimal?)p.Amount) ?? 0m,
                TotalAdjustments = _db.Set<RetailPosAdjustment2>().Where(a => a.ClientId == c.Id).Sum(a => (decimal?)a.Amount) ?? 0m
            });
        }

        private static void SetBalance(RetailPosClientRowDto2 row)
        {
            row.Balance = row.OpeningBalance + row.TotalSales + row.TotalAdjustments - row.TotalPaid;
        }
    }
}
