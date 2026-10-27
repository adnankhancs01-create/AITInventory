using Common.RetailPos2;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class RetailPosPaymentRepo2 : IRetailPosPaymentRepo2
    {
        private readonly InventoryDbContext _db;

        public RetailPosPaymentRepo2(InventoryDbContext db)
        {
            _db = db;
        }

        public Task<int> CountPaymentsWithPrefixAsync(string numberPrefix)
        {
            return _db.Set<RetailPosPayment2>().CountAsync(p => p.PaymentNumber.StartsWith(numberPrefix));
        }

        public Task<RetailPosPayment2?> FindPaymentByKeyAsync(Guid idempotencyKey)
        {
            return _db.Set<RetailPosPayment2>().AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey);
        }

        public void AddPayment(RetailPosPayment2 payment)
        {
            _db.Set<RetailPosPayment2>().Add(payment);
        }

        public Task<RetailPosPayment2?> GetPaymentAsync(int paymentId)
        {
            return _db.Set<RetailPosPayment2>().FirstOrDefaultAsync(p => p.Id == paymentId);
        }

        public Task<RetailPosPaymentRowDto2?> GetPaymentRowAsync(int paymentId)
        {
            var query = _db.Set<RetailPosPayment2>().AsNoTracking().Where(p => p.Id == paymentId);
            return ProjectRows(query).FirstOrDefaultAsync();
        }

        public Task<List<RetailPosPaymentRowDto2>> GetPaymentRowsAsync(RetailPosPaymentFilterDto2 filter)
        {
            var query = _db.Set<RetailPosPayment2>().AsNoTracking().AsQueryable();

            if (filter.ClientId.HasValue) query = query.Where(p => p.ClientId == filter.ClientId.Value);
            if (filter.SaleId.HasValue) query = query.Where(p => p.SaleId == filter.SaleId.Value);
            if (filter.FromDate.HasValue) { var f = filter.FromDate.Value.Date; query = query.Where(p => p.PaymentDate >= f); }
            if (filter.ToDate.HasValue) { var t = filter.ToDate.Value.Date.AddDays(1); query = query.Where(p => p.PaymentDate < t); }
            if (!string.IsNullOrWhiteSpace(filter.Method)) query = query.Where(p => p.Method == filter.Method);
            if (!filter.IncludeReversed) query = query.Where(p => !p.IsReversed);

            return ProjectRows(query).OrderByDescending(r => r.PaymentDate).ThenByDescending(r => r.Id).ToListAsync();
        }

        public void AddAdjustment(RetailPosAdjustment2 adjustment)
        {
            _db.Set<RetailPosAdjustment2>().Add(adjustment);
        }

        public Task<List<RetailPosAdjustment2>> GetAdjustmentsAsync(int clientId)
        {
            return _db.Set<RetailPosAdjustment2>().AsNoTracking()
                .Where(a => a.ClientId == clientId)
                .OrderBy(a => a.CreatedOn).ThenBy(a => a.Id)
                .ToListAsync();
        }

        public async Task<Dictionary<int, string>> GetUserNamesAsync(ICollection<int> userIds)
        {
            if (userIds.Count == 0) return new Dictionary<int, string>();

            var users = await _db.UserMst.AsNoTracking()
                .Where(u => userIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.UserName, u.Email })
                .ToListAsync();

            return users.ToDictionary(u => u.UserId, u => u.UserName ?? u.Email ?? string.Empty);
        }

        private IQueryable<RetailPosPaymentRowDto2> ProjectRows(IQueryable<RetailPosPayment2> query)
        {
            return query.Select(p => new RetailPosPaymentRowDto2
            {
                Id = p.Id,
                PaymentNumber = p.PaymentNumber,
                PaymentDate = p.PaymentDate,
                ClientId = p.ClientId,
                ClientName = p.Client != null ? p.Client.Name : string.Empty,
                SaleId = p.SaleId,
                InvoiceNumber = p.Sale != null ? p.Sale.InvoiceNumber : string.Empty,
                Amount = p.Amount,
                Method = p.Method,
                Reference = p.Reference ?? string.Empty,
                Remarks = p.Remarks ?? string.Empty,
                IsReversed = p.IsReversed,
                ReversedOn = p.ReversedOn,
                ReversalReason = p.ReversalReason ?? string.Empty,
                ReceivedBy = _db.UserMst.Where(u => u.UserId == p.CreatedBy).Select(u => u.UserName).FirstOrDefault() ?? string.Empty
            });
        }
    }
}
