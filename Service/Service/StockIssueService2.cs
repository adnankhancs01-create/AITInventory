using Common;
using Common.Stock2;
using Domain.Entities;
using Domain.IRepositories;
using Domain.Stock2;
using Service.IService;

namespace Service.Service
{
    /// <summary>Non-sale stock issues (damage, expiry, loss, internal use...). Posts negative Stock rows.</summary>
    public class StockIssueService2 : IStockIssueService2
    {
        private readonly IStockUnitOfWorkFactory2 _uowFactory;
        private readonly ILogRepository _logRepo;

        public StockIssueService2(IStockUnitOfWorkFactory2 uowFactory, ILogRepository logRepo)
        {
            _uowFactory = uowFactory;
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

                await using var uow = _uowFactory.Create();
                await uow.Ledger.EnsureDefaultLocationAsync();

                var location = await uow.Ledger.GetLocationAsync(request.LocationId, true);
                if (location == null) return StockResult2.Fail<int>("Location not found or inactive.");

                var productIds = wanted.Keys.ToList();
                var names = await uow.Ledger.GetActiveProductNamesAsync(productIds);
                foreach (var pid in productIds.Where(pid => !names.ContainsKey(pid)))
                    errors.Add($"Product {pid} was not found or is inactive.");
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                await uow.BeginTransactionAsync();

                // ---- availability at the chosen location (checked inside the transaction)
                foreach (var pair in wanted)
                {
                    int available = await uow.Ledger.GetQuantityAtLocationAsync(pair.Key, location);
                    if (pair.Value > available)
                        errors.Add($"'{names[pair.Key]}': only {Math.Max(available, 0)} available at {location.Name}, but {pair.Value} requested.");
                }
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                var states = await uow.Ledger.GetStatesAsync(productIds);

                string prefix = StockLedgerRules2.DocumentPrefix("ISS", date);
                int existing = await uow.Issues.CountIssuesWithPrefixAsync(prefix);

                var issue = new StockIssue2
                {
                    IssueNumber = StockLedgerRules2.BuildDocumentNumber("ISS", date, existing),
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

                    var row = StockLedgerRules2.NewStockOutRow(pair.Key, pair.Value, userId);
                    uow.Ledger.AddLedgerRow(row);

                    issue.Lines.Add(new StockIssueLine2
                    {
                        ProductId = pair.Key,
                        Quantity = pair.Value,
                        UnitCost = unitCost,
                        VendorStock = row
                    });

                    await uow.Ledger.ChangeBalanceAsync(pair.Key, location, -pair.Value);
                    created.Add((pair.Key, pair.Value, unitCost, row));
                }

                uow.Issues.AddIssue(issue);
                await uow.SaveChangesAsync(); // issue now has its Id

                foreach (var c in created)
                {
                    uow.Ledger.AddMovement(StockLedgerRules2.NewMovement(
                        c.ProductId, location.Id, StockConstants2.MoveIssue, -c.Quantity, c.UnitCost,
                        StockConstants2.DocIssue, issue.Id, issue.IssueNumber, c.Row,
                        issue.Reason, issue.Remarks, userId));
                }

                await uow.SaveChangesAsync();
                await uow.CommitAsync();

                return StockResult2.Ok(issue.Id, $"Issue {issue.IssueNumber} posted.");
            }, _logRepo, nameof(PostIssueAsync), userId, request);
        }

        public Task<BaseResponse<PagedResult2<DocumentRowDto2>>> GetIssuesAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();
                var rows = await uow.Issues.GetIssueRowsAsync(filter);

                var page = StockPaging2.ToPage(rows, filter, nameof(DocumentRowDto2.Date), true);
                page.Totals["TotalValue"] = rows.Sum(r => r.TotalValue);
                return StockResult2.Ok(page);
            }, _logRepo, nameof(GetIssuesAsync), null, filter);
        }

        public Task<BaseResponse<List<DocumentLineDto2>>> GetIssueLinesAsync(int issueId)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();
                var lines = await uow.Issues.GetIssueLinesAsync(issueId);
                return StockResult2.Ok(lines);
            }, _logRepo, nameof(GetIssueLinesAsync), null, new { issueId });
        }
    }
}
