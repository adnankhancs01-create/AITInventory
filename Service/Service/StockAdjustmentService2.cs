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
    /// <summary>
    /// Stock adjustments and physical-count reconciliation. The user enters the counted quantity per product;
    /// the service compares it with the system quantity at the location (read inside the transaction)
    /// and posts only the difference to the VendorStock ledger.
    /// </summary>
    public class StockAdjustmentService2 : IStockAdjustmentService2
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogRepository _logRepo;

        public StockAdjustmentService2(IServiceScopeFactory scopeFactory, ILogRepository logRepo)
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

        public Task<BaseResponse<int>> PostAdjustmentAsync(AdjustmentRequestDto2 request)
        {
            int userId = request.UserId ?? StockConstants2.SystemUserId;

            return StockResult2.RunAsync(async () =>
            {
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

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                await StockLedgerHelper2.EnsureDefaultLocationAsync(db);

                var location = await db.StockLocations2.FirstOrDefaultAsync(l => l.Id == request.LocationId && l.IsActive);
                if (location == null) return StockResult2.Fail<int>("Location not found or inactive.");

                var productIds = request.Lines.Select(l => l.ProductId).ToList();
                var names = await StockLedgerHelper2.LoadActiveProductNamesAsync(db, productIds);
                foreach (var pid in productIds.Where(pid => !names.ContainsKey(pid)))
                    errors.Add($"Product {pid} was not found or is inactive.");
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                await using var tx = await db.Database.BeginTransactionAsync();

                var states = await StockLedgerHelper2.LoadStatesAsync(db, productIds);

                string prefix = $"ADJ-{date:yyyyMMdd}-";
                int existing = await db.StockAdjustments2.CountAsync(a => a.AdjustmentNumber.StartsWith(prefix));

                var adjustment = new StockAdjustment2
                {
                    AdjustmentNumber = StockLedgerHelper2.BuildDocumentNumber("ADJ", date, existing),
                    AdjustmentDate = date,
                    LocationId = location.Id,
                    Reason = StockConstants2.AdjustmentReasons.First(r => r.Equals(reason, StringComparison.OrdinalIgnoreCase)),
                    Remarks = Clean(request.Remarks, 500),
                    Status = StockConstants2.StatusPosted,
                    CreatedBy = userId,
                    CreatedOn = DateTime.Now
                };

                var movements = new List<(StockAdjustmentLine2 Line, VendorStock Row)>();

                foreach (var input in request.Lines)
                {
                    int counted = input.CountedQuantity!.Value;
                    int system = await StockLedgerHelper2.GetQuantityAtLocationAsync(db, input.ProductId, location);
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
                        VendorStock row;
                        if (diff > 0)
                        {
                            // Found stock: value it at the last known unit cost.
                            decimal cost = state?.LastUnitCost ?? 0m;
                            line.UnitCost = cost;
                            row = StockLedgerHelper2.NewStockInRow(input.ProductId, diff, Math.Round(cost * diff, 2), null, userId);
                        }
                        else
                        {
                            line.UnitCost = StockLayerCalculator2.EstimateIssueCost(state, -diff);
                            row = StockLedgerHelper2.NewStockOutRow(input.ProductId, -diff, userId);
                        }

                        db.VendorStock.Add(row);
                        line.VendorStock = row;
                        await StockLedgerHelper2.ChangeBalanceAsync(db, input.ProductId, location, diff);
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

                db.StockAdjustments2.Add(adjustment);
                await db.SaveChangesAsync();

                foreach (var m in movements)
                {
                    db.StockMovements2.Add(StockLedgerHelper2.NewMovement(
                        m.Line.ProductId, location.Id,
                        m.Line.Difference > 0 ? StockConstants2.MoveAdjustmentIn : StockConstants2.MoveAdjustmentOut,
                        m.Line.Difference, m.Line.UnitCost,
                        StockConstants2.DocAdjustment, adjustment.Id, adjustment.AdjustmentNumber, m.Row,
                        adjustment.Reason, adjustment.Remarks, userId));
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return StockResult2.Ok(adjustment.Id, $"Adjustment {adjustment.AdjustmentNumber} posted.");
            }, _logRepo, nameof(PostAdjustmentAsync), userId, request);
        }

        public Task<BaseResponse<PagedResult2<DocumentRowDto2>>> GetAdjustmentsAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var query = db.StockAdjustments2.AsNoTracking().AsQueryable();
                if (filter.LocationId.HasValue) query = query.Where(a => a.LocationId == filter.LocationId.Value);
                if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(a => a.Reason == filter.Status);
                if (filter.FromDate.HasValue) { var f = filter.FromDate.Value.Date; query = query.Where(a => a.AdjustmentDate >= f); }
                if (filter.ToDate.HasValue) { var e = filter.ToDate.Value.Date.AddDays(1); query = query.Where(a => a.AdjustmentDate < e); }
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    string s = filter.Search.Trim();
                    query = query.Where(a => a.AdjustmentNumber.Contains(s) || a.Reason.Contains(s));
                }

                var rows = await query.Select(a => new DocumentRowDto2
                {
                    Id = a.Id,
                    Number = a.AdjustmentNumber,
                    Date = a.AdjustmentDate,
                    Status = a.Status,
                    Party = a.Location!.Name,
                    Reason = a.Reason,
                    LineCount = a.Lines.Count,
                    TotalQuantity = a.Lines.Sum(l => l.Difference),
                    TotalValue = a.Lines.Sum(l => l.Difference * l.UnitCost),
                    Remarks = a.Remarks ?? string.Empty
                }).ToListAsync();

                var page = StockPaging2.ToPage(rows, filter, nameof(DocumentRowDto2.Date), true);
                page.Totals["TotalValue"] = rows.Sum(r => r.TotalValue);
                return StockResult2.Ok(page);
            }, _logRepo, nameof(GetAdjustmentsAsync), null, filter);
        }

        public Task<BaseResponse<List<DocumentLineDto2>>> GetAdjustmentLinesAsync(int adjustmentId)
        {
            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var lines = await db.StockAdjustmentLines2.AsNoTracking()
                    .Where(l => l.AdjustmentId == adjustmentId)
                    .OrderBy(l => l.Id)
                    .Select(l => new DocumentLineDto2
                    {
                        LineId = l.Id,
                        ProductId = l.ProductId,
                        ProductCode = l.Product!.ProductCode,
                        ProductName = l.Product!.Name ?? string.Empty,
                        Quantity = l.CountedQuantity,
                        ProcessedQuantity = l.SystemQuantity,
                        UnitCost = l.UnitCost,
                        Note = l.Remarks ?? string.Empty
                    }).ToListAsync();

                return StockResult2.Ok(lines);
            }, _logRepo, nameof(GetAdjustmentLinesAsync), null, new { adjustmentId });
        }

        public Task<BaseResponse<List<DocumentLineDto2>>> GetCountSheetAsync(int locationId, int? categoryId, string? search)
        {
            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                await StockLedgerHelper2.EnsureDefaultLocationAsync(db);

                var location = await db.StockLocations2.AsNoTracking().FirstOrDefaultAsync(l => l.Id == locationId && l.IsActive);
                if (location == null) return StockResult2.Fail<List<DocumentLineDto2>>("Location not found or inactive.");

                var products = db.Products.AsNoTracking().Where(p => p.IsActive);
                if (categoryId.HasValue) products = products.Where(p => p.CategoryId == categoryId.Value);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.Trim();
                    products = products.Where(p => p.ProductCode.Contains(s) || (p.Name != null && p.Name.Contains(s)));
                }

                var list = await products.OrderBy(p => p.ProductCode)
                    .Select(p => new { p.Id, p.ProductCode, Name = p.Name ?? p.ProductCode })
                    .ToListAsync();

                var ids = list.Select(p => p.Id).ToList();
                var onHandRows = await StockLedgerHelper2.LoadActiveLedgerAsync(db);
                var onHand = onHandRows.GroupBy(r => r.ProductId).ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));
                var balances = await StockLedgerHelper2.LoadBalanceTotalsAsync(db);
                var transit = await StockLedgerHelper2.LoadInTransitAsync(db);

                Dictionary<int, int> atLocation = new();
                if (!location.IsDefault)
                {
                    atLocation = await db.StockLocationBalances2.AsNoTracking()
                        .Where(b => b.LocationId == locationId)
                        .ToDictionaryAsync(b => b.ProductId, b => b.Quantity);
                }

                var result = new List<DocumentLineDto2>();
                foreach (var p in list)
                {
                    int qty = StockLedgerHelper2.QuantityAtLocation(
                        StockLedgerHelper2.Get(onHand, p.Id),
                        StockLedgerHelper2.Get(balances, p.Id),
                        StockLedgerHelper2.Get(transit, p.Id),
                        location.IsDefault,
                        StockLedgerHelper2.Get(atLocation, p.Id));

                    result.Add(new DocumentLineDto2
                    {
                        ProductId = p.Id,
                        ProductCode = p.ProductCode,
                        ProductName = p.Name,
                        ProcessedQuantity = qty,
                        Quantity = qty
                    });
                }

                return StockResult2.Ok(result);
            }, _logRepo, nameof(GetCountSheetAsync), null, new { locationId, categoryId, search });
        }
    }
}
