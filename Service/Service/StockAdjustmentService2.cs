using Common;
using Common.Stock2;
using Domain.Entities;
using Domain.IRepositories;
using Domain.Stock2;
using Domain.Users2;
using Service.IService;

namespace Service.Service
{
    /// <summary>
    /// Stock adjustments and physical-count reconciliation. The user enters the counted quantity per product;
    /// the service compares it with the system quantity at the location (read inside the transaction)
    /// and posts only the difference to the Stock ledger.
    /// </summary>
    public class StockAdjustmentService2 : IStockAdjustmentService2
    {
        private readonly IStockUnitOfWorkFactory2 _uowFactory;
        private readonly ILogRepository _logRepo;
        private readonly ICurrentUser2 _currentUser;

        public StockAdjustmentService2(IStockUnitOfWorkFactory2 uowFactory, ILogRepository logRepo, ICurrentUser2 currentUser)
        {
            _uowFactory = uowFactory;
            _logRepo = logRepo;
            _currentUser = currentUser;
        }

        private static string? Clean(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }

        public Task<BaseResponse<int>> PostAdjustmentAsync(AdjustmentRequestDto2 request)
        {
            int userId = StockAccess2.ActingUserId(_currentUser, request.UserId);

            return StockResult2.RunAsync(async () =>
            {
                var denied = StockAccess2.Check<int>(_currentUser, ScreenCatalog2.StockAdjustments);
                if (denied != null) return denied;

                DateTime date = request.DocumentDate ?? DateTime.Now;
                var errors = new List<string>();

                string reason = (request.Reason ?? string.Empty).Trim();
                if (!StockConstants2.AdjustmentReasons.Contains(reason, StringComparer.OrdinalIgnoreCase))
                    errors.Add("Choose a valid reason.");
                if (request.LocationId <= 0) errors.Add("Choose a location.");
                if (date > DateTime.Now.AddMinutes(5)) errors.Add("The adjustment date cannot be in the future.");

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
                        if (!line.CountedQuantity.HasValue || line.CountedQuantity.Value < 0)
                            errors.Add(n + "counted quantity must be zero or more.");
                    }
                    if (request.Lines.GroupBy(l => l.ProductId).Any(g => g.Count() > 1))
                        errors.Add("A product can appear only once in an adjustment.");
                }
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                await using var uow = _uowFactory.Create();
                await uow.Ledger.EnsureDefaultLocationAsync();

                var location = await uow.Ledger.GetLocationAsync(request.LocationId, true);
                if (location == null) return StockResult2.Fail<int>("Location not found or inactive.");

                var productIds = request.Lines.Select(l => l.ProductId).ToList();
                var names = await uow.Ledger.GetActiveProductNamesAsync(productIds);
                foreach (var pid in productIds.Where(pid => !names.ContainsKey(pid)))
                    errors.Add($"Product {pid} was not found or is inactive.");
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                await uow.BeginTransactionAsync();

                var states = await uow.Ledger.GetStatesAsync(productIds);

                string prefix = StockLedgerRules2.DocumentPrefix("ADJ", date);
                int existing = await uow.Adjustments.CountAdjustmentsWithPrefixAsync(prefix);

                var adjustment = new StockAdjustment2
                {
                    AdjustmentNumber = StockLedgerRules2.BuildDocumentNumber("ADJ", date, existing),
                    AdjustmentDate = date,
                    LocationId = location.Id,
                    Reason = StockConstants2.AdjustmentReasons.First(r => r.Equals(reason, StringComparison.OrdinalIgnoreCase)),
                    Remarks = Clean(request.Remarks, 500),
                    Status = StockConstants2.StatusPosted,
                    CreatedBy = userId,
                    CreatedOn = DateTime.Now
                };

                var movements = new List<(StockAdjustmentLine2 Line, Stock Row)>();

                foreach (var input in request.Lines)
                {
                    int counted = input.CountedQuantity!.Value;
                    int system = await uow.Ledger.GetQuantityAtLocationAsync(input.ProductId, location);
                    int diff = counted - system;

                    states.TryGetValue(input.ProductId, out var state);

                    var line = new StockAdjustmentLine2
                    {
                        ProductId = input.ProductId,
                        SystemQuantity = system,
                        CountedQuantity = counted,
                        Difference = diff,
                        Remarks = Clean(input.Remarks, 250)
                    };

                    if (diff != 0)
                    {
                        Stock row;
                        if (diff > 0)
                        {
                            // Found stock: value it at the last known unit cost.
                            decimal cost = state?.LastUnitCost ?? 0m;
                            line.UnitCost = cost;
                            row = StockLedgerRules2.NewStockInRow(input.ProductId, diff, Math.Round(cost * diff, 2), null, userId);
                        }
                        else
                        {
                            line.UnitCost = StockLayerCalculator2.EstimateIssueCost(state, -diff);
                            row = StockLedgerRules2.NewStockOutRow(input.ProductId, -diff, userId);
                        }

                        uow.Ledger.AddLedgerRow(row);
                        line.VendorStock = row;
                        await uow.Ledger.ChangeBalanceAsync(input.ProductId, location, diff);
                        movements.Add((line, row));
                    }
                    else
                    {
                        line.UnitCost = state?.LastUnitCost ?? 0m;
                    }

                    adjustment.Lines.Add(line);
                }

                if (movements.Count == 0)
                    return StockResult2.Fail<int>("Every counted quantity equals the system quantity. There is nothing to adjust.");

                uow.Adjustments.AddAdjustment(adjustment);
                await uow.SaveChangesAsync();

                foreach (var m in movements)
                {
                    uow.Ledger.AddMovement(StockLedgerRules2.NewMovement(
                        m.Line.ProductId, location.Id,
                        m.Line.Difference > 0 ? StockConstants2.MoveAdjustmentIn : StockConstants2.MoveAdjustmentOut,
                        m.Line.Difference, m.Line.UnitCost,
                        StockConstants2.DocAdjustment, adjustment.Id, adjustment.AdjustmentNumber, m.Row,
                        adjustment.Reason, adjustment.Remarks, userId));
                }

                await uow.SaveChangesAsync();
                await uow.CommitAsync();

                return StockResult2.Ok(adjustment.Id, $"Adjustment {adjustment.AdjustmentNumber} posted.");
            }, _logRepo, nameof(PostAdjustmentAsync), userId, request);
        }

        public Task<BaseResponse<PagedResult2<DocumentRowDto2>>> GetAdjustmentsAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();
                var rows = await uow.Adjustments.GetAdjustmentRowsAsync(filter);

                var page = StockPaging2.ToPage(rows, filter, nameof(DocumentRowDto2.Date), true);
                page.Totals["TotalValue"] = rows.Sum(r => r.TotalValue);
                return StockResult2.Ok(page);
            }, _logRepo, nameof(GetAdjustmentsAsync), null, filter);
        }

        public Task<BaseResponse<List<DocumentLineDto2>>> GetAdjustmentLinesAsync(int adjustmentId)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();
                var lines = await uow.Adjustments.GetAdjustmentLinesAsync(adjustmentId);
                return StockResult2.Ok(lines);
            }, _logRepo, nameof(GetAdjustmentLinesAsync), null, new { adjustmentId });
        }

        public Task<BaseResponse<List<DocumentLineDto2>>> GetCountSheetAsync(int locationId, int? categoryId, string? search)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();
                await uow.Ledger.EnsureDefaultLocationAsync();

                var location = await uow.Ledger.GetLocationAsync(locationId, true);
                if (location == null) return StockResult2.Fail<List<DocumentLineDto2>>("Location not found or inactive.");

                var productFilter = new StockQueryFilter2 { CategoryId = categoryId, Search = search };
                var list = (await uow.Reports.GetProductsAsync(productFilter, false))
                    .OrderBy(p => p.Code, StringComparer.Ordinal)
                    .ToList();

                var states = await uow.Ledger.GetStatesAsync();
                var balances = await uow.Ledger.GetBalanceTotalsAsync();
                var transit = await uow.Ledger.GetInTransitAsync();

                Dictionary<int, int> atLocation = new();
                if (!location.IsDefault)
                    atLocation = await uow.Ledger.GetLocationBalancesAsync(locationId);

                var result = new List<DocumentLineDto2>();
                foreach (var p in list)
                {
                    states.TryGetValue(p.Id, out var state);

                    int qty = StockLedgerRules2.QuantityAtLocation(
                        state?.OnHand ?? 0,
                        StockLedgerRules2.Get(balances, p.Id),
                        StockLedgerRules2.Get(transit, p.Id),
                        location.IsDefault,
                        StockLedgerRules2.Get(atLocation, p.Id));

                    result.Add(new DocumentLineDto2
                    {
                        ProductId = p.Id,
                        ProductCode = p.Code,
                        ProductName = string.IsNullOrEmpty(p.Name) ? p.Code : p.Name,
                        ProcessedQuantity = qty,
                        Quantity = qty
                    });
                }

                return StockResult2.Ok(result);
            }, _logRepo, nameof(GetCountSheetAsync), null, new { locationId, categoryId, search });
        }
    }
}
