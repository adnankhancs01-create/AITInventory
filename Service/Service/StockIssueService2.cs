using Common;
using Common.Stock2;
using Data;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.IService;

namespace Service.Service
{
    /// <summary>Non-sale stock issues (damage, expiry, loss, internal use...). Posts negative VendorStock rows.</summary>
    public class StockIssueService2 : IStockIssueService2
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogRepository _logRepo;

        public StockIssueService2(IServiceScopeFactory scopeFactory, ILogRepository logRepo)
        {
            _scopeFactory = scopeFactory;
            _logRepo = logRepo;
        }

        private static string? Clean(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }

        public Task<BaseResponse<int>> PostIssueAsync(IssueRequestDto2 request)
        {
            int userId = request.UserId ?? StockConstants2.SystemUserId;

            return StockResult2.RunAsync(async () =>
            {
                DateTime date = request.DocumentDate ?? DateTime.Now;
                var errors = new List<string>();

                string reason = (request.Reason ?? string.Empty).Trim();
                if (!StockConstants2.IssueReasons.Contains(reason, StringComparer.OrdinalIgnoreCase))
                    errors.Add("Choose a valid reason.");
                if (request.LocationId <= 0) errors.Add("Choose a location.");
                if (date > DateTime.Now.AddMinutes(5)) errors.Add("The issue date cannot be in the future.");

                if (request.Lines == null || request.Lines.Count == 0)
                {
                    errors.Add("Add at least one product line.");
                }
                else
                {
                    for (int i = 0; i < request.Lines.Count; i++)
                    {
                        var line = request.Lines[i];
                        string n = $"Line {i + 1}: ";
                        if (line.ProductId <= 0) errors.Add(n + "choose a product.");
                        if (line.Quantity <= 0) errors.Add(n + "quantity must be greater than zero.");
                    }
                }
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                // One line per product (quantities of repeated products are added together).
                var wanted = request.Lines.GroupBy(l => l.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                await StockLedgerHelper2.EnsureDefaultLocationAsync(db);

                var location = await db.StockLocations2.FirstOrDefaultAsync(l => l.Id == request.LocationId && l.IsActive);
                if (location == null) return StockResult2.Fail<int>("Location not found or inactive.");

                var productIds = wanted.Keys.ToList();
                var names = await StockLedgerHelper2.LoadActiveProductNamesAsync(db, productIds);
                foreach (var pid in productIds.Where(pid => !names.ContainsKey(pid)))
                    errors.Add($"Product {pid} was not found or is inactive.");
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                await using var tx = await db.Database.BeginTransactionAsync();

                // ---- availability at the chosen location (checked inside the transaction)
                foreach (var pair in wanted)
                {
                    int available = await StockLedgerHelper2.GetQuantityAtLocationAsync(db, pair.Key, location);
                    if (pair.Value > available)
                        errors.Add($"'{names[pair.Key]}': only {Math.Max(available, 0)} available at {location.Name}, but {pair.Value} requested.");
                }
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                var states = await StockLedgerHelper2.LoadStatesAsync(db, productIds);

                string prefix = $"ISS-{date:yyyyMMdd}-";
                int existing = await db.StockIssues2.CountAsync(i => i.IssueNumber.StartsWith(prefix));

                var issue = new StockIssue2
                {
                    IssueNumber = StockLedgerHelper2.BuildDocumentNumber("ISS", date, existing),
                    IssueDate = date,
                    LocationId = location.Id,
                    Reason = StockConstants2.IssueReasons.First(r => r.Equals(reason, StringComparison.OrdinalIgnoreCase)),
                    Remarks = Clean(request.Remarks, 500),
                    Status = StockConstants2.StatusPosted,
                    CreatedBy = userId,
                    CreatedOn = DateTime.Now
                };

                var created = new List<(int ProductId, int Quantity, decimal UnitCost, Stock Row)>();
                foreach (var pair in wanted)
                {
                    states.TryGetValue(pair.Key, out var state);
                    decimal unitCost = StockLayerCalculator2.EstimateIssueCost(state, pair.Value);

                    var row = StockLedgerHelper2.NewStockOutRow(pair.Key, pair.Value, userId);
                    db.Stock.Add(row);

                    issue.Lines.Add(new StockIssueLine2
                    {
                        ProductId = pair.Key,
                        Quantity = pair.Value,
                        UnitCost = unitCost,
                        VendorStock = row
                    });

                    await StockLedgerHelper2.ChangeBalanceAsync(db, pair.Key, location, -pair.Value);
                    created.Add((pair.Key, pair.Value, unitCost, row));
                }

                db.StockIssues2.Add(issue);
                await db.SaveChangesAsync(); // issue now has its Id

                foreach (var c in created)
                {
                    db.StockMovements2.Add(StockLedgerHelper2.NewMovement(
                        c.ProductId, location.Id, StockConstants2.MoveIssue, -c.Quantity, c.UnitCost,
                        StockConstants2.DocIssue, issue.Id, issue.IssueNumber, c.Row,
                        issue.Reason, issue.Remarks, userId));
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return StockResult2.Ok(issue.Id, $"Issue {issue.IssueNumber} posted.");
            }, _logRepo, nameof(PostIssueAsync), userId, request);
        }

        public Task<BaseResponse<PagedResult2<DocumentRowDto2>>> GetIssuesAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var query = db.StockIssues2.AsNoTracking().AsQueryable();
                if (filter.LocationId.HasValue) query = query.Where(i => i.LocationId == filter.LocationId.Value);
                if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(i => i.Reason == filter.Status);
                if (filter.FromDate.HasValue) { var f = filter.FromDate.Value.Date; query = query.Where(i => i.IssueDate >= f); }
                if (filter.ToDate.HasValue) { var t = filter.ToDate.Value.Date.AddDays(1); query = query.Where(i => i.IssueDate < t); }
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    string s = filter.Search.Trim();
                    query = query.Where(i => i.IssueNumber.Contains(s) || i.Reason.Contains(s));
                }

                var rows = await query.Select(i => new DocumentRowDto2
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

                var page = StockPaging2.ToPage(rows, filter, nameof(DocumentRowDto2.Date), true);
                page.Totals["TotalValue"] = rows.Sum(r => r.TotalValue);
                return StockResult2.Ok(page);
            }, _logRepo, nameof(GetIssuesAsync), null, filter);
        }

        public Task<BaseResponse<List<DocumentLineDto2>>> GetIssueLinesAsync(int issueId)
        {
            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var lines = await db.StockIssueLines2.AsNoTracking()
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

                return StockResult2.Ok(lines);
            }, _logRepo, nameof(GetIssueLinesAsync), null, new { issueId });
        }
    }
}
