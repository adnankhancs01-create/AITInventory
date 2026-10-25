using Common;
using Common.Stock2;
using Domain.Entities;
using Domain.IRepositories;
using Domain.Stock2;
using Service.IService;

namespace Service.Service
{
    /// <summary>Dashboards, inventory overview and reports. Everything is derived from the Stock ledger plus the Stock2 tables.</summary>
    public class StockQueryService2 : IStockQueryService2
    {
        private readonly IStockUnitOfWorkFactory2 _uowFactory;
        private readonly ILogRepository _logRepo;

        public StockQueryService2(IStockUnitOfWorkFactory2 uowFactory, ILogRepository logRepo)
        {
            _uowFactory = uowFactory;
            _logRepo = logRepo;
        }

        private static bool HasProductFilter(StockQueryFilter2 filter)
        {
            return filter.ProductId.HasValue || filter.CategoryId.HasValue || !string.IsNullOrWhiteSpace(filter.Search);
        }

        // ================================================================== overview

        private static async Task<List<StockOverviewRowDto2>> BuildOverviewRowsAsync(IStockUnitOfWork2 uow, StockQueryFilter2 filter)
        {
            var products = await uow.Reports.GetProductsAsync(filter, false);
            ICollection<int>? scope = HasProductFilter(filter) ? products.Select(p => p.Id).ToList() : null;

            var states = await uow.Ledger.GetStatesAsync(scope);
            var balances = await uow.Ledger.GetBalanceTotalsAsync(scope);
            var transit = await uow.Ledger.GetInTransitAsync(scope);
            var settings = await uow.Master.GetSettingsByProductAsync();

            StockLocation2? location = null;
            var locationBalances = new Dictionary<int, int>();
            if (filter.LocationId.HasValue)
            {
                location = await uow.Reports.FindLocationAsync(filter.LocationId.Value);
                if (location != null && !location.IsDefault)
                    locationBalances = await uow.Ledger.GetLocationBalancesAsync(location.Id);
            }

            var priceByCode = await uow.Reports.GetLatestSellPricesAsync();
            var lastByProduct = await uow.Reports.GetLastMovementDatesAsync();

            var rows = new List<StockOverviewRowDto2>();
            foreach (var p in products)
            {
                states.TryGetValue(p.Id, out var state);
                settings.TryGetValue(p.Id, out var setting);

                int onHand = state?.OnHand ?? 0;
                int otherBalances = StockLedgerRules2.Get(balances, p.Id);
                int inTransit = StockLedgerRules2.Get(transit, p.Id);
                int reorderLevel = setting?.ReorderLevel ?? 0;
                decimal avgCost = state?.AverageUnitCost ?? 0m;

                int quantityAtLocation = onHand;
                decimal value = state?.StockValue ?? 0m;
                if (location != null)
                {
                    locationBalances.TryGetValue(p.Id, out var lb);
                    quantityAtLocation = StockLedgerRules2.QuantityAtLocation(onHand, otherBalances, inTransit, location.IsDefault, lb);
                    value = Math.Max(quantityAtLocation, 0) * avgCost;
                }

                if (!filter.IncludeZero && quantityAtLocation == 0) continue;
                if (filter.OnlyLowStock && !(reorderLevel > 0 && onHand <= reorderLevel)) continue;

                string status = onHand < 0 ? StockConstants2.StockNegative
                    : onHand == 0 ? StockConstants2.StockOut
                    : (reorderLevel > 0 && onHand <= reorderLevel) ? StockConstants2.StockLow
                    : StockConstants2.StockOk;

                if (!string.IsNullOrWhiteSpace(filter.Status) && status != filter.Status) continue;

                priceByCode.TryGetValue(p.Code, out var price);
                DateTime? last = lastByProduct.TryGetValue(p.Id, out var l) ? l : null;

                rows.Add(new StockOverviewRowDto2
                {
                    ProductId = p.Id,
                    ProductCode = p.Code,
                    ProductName = p.Name,
                    CategoryName = p.Category,
                    OnHand = onHand,
                    QuantityAtLocation = quantityAtLocation,
                    InTransit = inTransit,
                    ReorderLevel = reorderLevel,
                    ReorderQuantity = setting?.ReorderQuantity ?? 0,
                    AverageUnitCost = Math.Round(avgCost, 4),
                    StockValue = Math.Round(value, 2),
                    SellPrice = price,
                    LastMovementOn = last,
                    StockStatus = status
                });
            }

            return rows;
        }

        public Task<BaseResponse<PagedResult2<StockOverviewRowDto2>>> GetOverviewAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();

                var rows = await BuildOverviewRowsAsync(uow, filter);
                var page = StockPaging2.ToPage(rows, filter, nameof(StockOverviewRowDto2.ProductName));
                page.Totals["StockValue"] = rows.Sum(r => r.StockValue);
                page.Totals["Units"] = rows.Sum(r => r.QuantityAtLocation);
                return StockResult2.Ok(page);
            }, _logRepo, nameof(GetOverviewAsync), null, filter);
        }

        // ================================================================== movements

        private static async Task<List<StockMovementRowDto2>> BuildMovementRowsAsync(IStockUnitOfWork2 uow, StockQueryFilter2 filter)
        {
            var products = await uow.Reports.GetProductsAsync(filter, true);
            var productMap = products.ToDictionary(p => p.Id);
            bool restrict = HasProductFilter(filter);
            List<int> productIds = productMap.Keys.ToList();

            var defaultLocation = await uow.Ledger.EnsureDefaultLocationAsync();
            var locationNames = await uow.Reports.GetLocationNamesAsync();

            DateTime? fromDate = filter.FromDate?.Date;
            DateTime? toExclusive = filter.ToDate?.Date.AddDays(1);

            // ---- A: every Stock ledger row (sales, purchases, returns, opening stock and Stock2 documents)
            var ledger = await uow.Reports.GetLedgerMovementRowsAsync(restrict ? productIds : null, fromDate, toExclusive);

            var ledgerIds = ledger.Select(l => l.Id).ToList();
            var linked = await uow.Reports.GetMovementsForLedgerRowsAsync(ledgerIds);
            var linkedByLedger = linked.GroupBy(m => m.VendorStockId!.Value).ToDictionary(g => g.Key, g => g.First());

            var result = new List<StockMovementRowDto2>();

            foreach (var l in ledger)
            {
                linkedByLedger.TryGetValue(l.Id, out var move);
                int? locationId = move?.LocationId ?? defaultLocation.Id;
                if (filter.LocationId.HasValue && locationId != filter.LocationId.Value) continue;

                string type;
                if (move != null) type = move.MovementType;
                else if (l.TransactionType == "Sell") type = StockConstants2.MoveSale;
                else if (l.TransactionType == "Purchase") type = StockConstants2.MovePurchase;
                else if (l.TransactionType == "Return") type = StockConstants2.MoveReturn;
                else if (l.Quantity > 0) type = StockConstants2.MoveOpening;
                else type = StockConstants2.MoveOther;

                if (!string.IsNullOrWhiteSpace(filter.MovementType) && type != filter.MovementType) continue;

                productMap.TryGetValue(l.ProductId, out var product);

                decimal? unitCost = move?.UnitCost;
                if (unitCost == null && l.Quantity > 0 && l.TotalPurchasePrice.GetValueOrDefault() > 0)
                    unitCost = Math.Round(l.TotalPurchasePrice!.Value / l.Quantity, 4);

                string reference = move?.DocumentNumber
                    ?? (l.TransactionNumber.HasValue ? l.TransactionNumber.Value.ToString() : null)
                    ?? (l.StockNumber.HasValue ? $"Stock {l.StockNumber.Value}" : string.Empty);

                result.Add(new StockMovementRowDto2
                {
                    MovementDate = l.CreatedOn,
                    ProductId = l.ProductId,
                    ProductCode = product?.Code ?? string.Empty,
                    ProductName = product?.Name ?? "(inactive product)",
                    MovementType = type,
                    Quantity = l.Quantity,
                    UnitCost = unitCost,
                    LocationName = locationId.HasValue && locationNames.TryGetValue(locationId.Value, out var ln) ? ln : string.Empty,
                    DocumentNumber = reference,
                    Reason = move?.Reason ?? string.Empty,
                    IsReversed = l.IsActive != true,
                    Source = "Ledger"
                });
            }

            // ---- B: location-only movements (transfer dispatch / receive / cancel) that have no ledger row
            var others = await uow.Reports.GetLocationOnlyMovementsAsync(
                restrict ? productIds : null, fromDate, toExclusive, filter.LocationId, filter.MovementType);

            foreach (var m in others)
            {
                productMap.TryGetValue(m.ProductId, out var product);
                result.Add(new StockMovementRowDto2
                {
                    MovementDate = m.CreatedOn,
                    ProductId = m.ProductId,
                    ProductCode = product?.Code ?? string.Empty,
                    ProductName = product?.Name ?? "(inactive product)",
                    MovementType = m.MovementType,
                    Quantity = m.Quantity,
                    UnitCost = m.UnitCost,
                    LocationName = m.LocationId.HasValue && locationNames.TryGetValue(m.LocationId.Value, out var ln) ? ln : string.Empty,
                    DocumentNumber = m.DocumentNumber ?? string.Empty,
                    Reason = m.Reason ?? string.Empty,
                    IsReversed = false,
                    Source = "Stock2"
                });
            }

            return result;
        }

        public Task<BaseResponse<PagedResult2<StockMovementRowDto2>>> GetMovementsAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();

                var rows = await BuildMovementRowsAsync(uow, filter);
                var page = StockPaging2.ToPage(rows, filter, nameof(StockMovementRowDto2.MovementDate), true);
                page.Totals["QuantityIn"] = rows.Where(r => !r.IsReversed && r.Quantity > 0).Sum(r => (decimal)r.Quantity);
                page.Totals["QuantityOut"] = rows.Where(r => !r.IsReversed && r.Quantity < 0).Sum(r => (decimal)-r.Quantity);
                return StockResult2.Ok(page);
            }, _logRepo, nameof(GetMovementsAsync), null, filter);
        }

        // ================================================================== valuation

        public Task<BaseResponse<PagedResult2<StockValuationRowDto2>>> GetValuationAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();

                var products = await uow.Reports.GetProductsAsync(filter, false);
                ICollection<int>? ids = HasProductFilter(filter) ? products.Select(p => p.Id).ToList() : null;
                var states = await uow.Ledger.GetStatesAsync(ids);

                var rows = new List<StockValuationRowDto2>();
                foreach (var p in products)
                {
                    states.TryGetValue(p.Id, out var state);
                    int onHand = state?.OnHand ?? 0;
                    if (!filter.IncludeZero && onHand <= 0) continue;

                    rows.Add(new StockValuationRowDto2
                    {
                        ProductId = p.Id,
                        ProductCode = p.Code,
                        ProductName = p.Name,
                        CategoryName = p.Category,
                        OnHand = onHand,
                        AverageUnitCost = Math.Round(state?.AverageUnitCost ?? 0m, 4),
                        StockValue = Math.Round(state?.StockValue ?? 0m, 2),
                        HasEstimatedCost = state?.HasEstimatedCost ?? false
                    });
                }

                var page = StockPaging2.ToPage(rows, filter, nameof(StockValuationRowDto2.StockValue), true);
                page.Totals["StockValue"] = rows.Sum(r => r.StockValue);
                page.Totals["Units"] = rows.Sum(r => (decimal)r.OnHand);
                return StockResult2.Ok(page);
            }, _logRepo, nameof(GetValuationAsync), null, filter);
        }

        // ================================================================== aging

        public Task<BaseResponse<PagedResult2<StockAgingRowDto2>>> GetAgingAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();

                var products = await uow.Reports.GetProductsAsync(filter, false);
                ICollection<int>? ids = HasProductFilter(filter) ? products.Select(p => p.Id).ToList() : null;
                var states = await uow.Ledger.GetStatesAsync(ids);
                DateTime today = DateTime.Today;

                var rows = new List<StockAgingRowDto2>();
                foreach (var p in products)
                {
                    if (!states.TryGetValue(p.Id, out var state) || state.Layers.Count == 0) continue;

                    var row = new StockAgingRowDto2 { ProductId = p.Id, ProductCode = p.Code, ProductName = p.Name };
                    foreach (var layer in state.Layers)
                    {
                        // Ledger rows without a date are treated as the oldest stock.
                        int age = layer.ReceivedOn == DateTime.MinValue ? int.MaxValue : Math.Max(0, (today - layer.ReceivedOn.Date).Days);
                        decimal value = layer.RemainingQuantity * layer.UnitCost;

                        if (age <= 30) row.Qty0To30 += layer.RemainingQuantity;
                        else if (age <= 60) row.Qty31To60 += layer.RemainingQuantity;
                        else if (age <= 90) row.Qty61To90 += layer.RemainingQuantity;
                        else
                        {
                            row.QtyOver90 += layer.RemainingQuantity;
                            row.ValueOver90 += value;
                        }

                        row.TotalQty += layer.RemainingQuantity;
                        row.TotalValue += value;
                    }

                    row.ValueOver90 = Math.Round(row.ValueOver90, 2);
                    row.TotalValue = Math.Round(row.TotalValue, 2);
                    row.OldestLayerDate = state.Layers
                        .Where(l => l.ReceivedOn != DateTime.MinValue)
                        .Select(l => (DateTime?)l.ReceivedOn).Min();
                    rows.Add(row);
                }

                var page = StockPaging2.ToPage(rows, filter, nameof(StockAgingRowDto2.QtyOver90), true);
                page.Totals["Qty0To30"] = rows.Sum(r => (decimal)r.Qty0To30);
                page.Totals["Qty31To60"] = rows.Sum(r => (decimal)r.Qty31To60);
                page.Totals["Qty61To90"] = rows.Sum(r => (decimal)r.Qty61To90);
                page.Totals["QtyOver90"] = rows.Sum(r => (decimal)r.QtyOver90);
                page.Totals["ValueOver90"] = rows.Sum(r => r.ValueOver90);
                page.Totals["TotalValue"] = rows.Sum(r => r.TotalValue);
                return StockResult2.Ok(page);
            }, _logRepo, nameof(GetAgingAsync), null, filter);
        }

        // ================================================================== expiry

        private static async Task<List<StockExpiryRowDto2>> BuildExpiryRowsAsync(IStockUnitOfWork2 uow, StockQueryFilter2 filter)
        {
            var products = await uow.Reports.GetProductsAsync(filter, false);
            var productMap = products.ToDictionary(p => p.Id);
            List<int> productIds = productMap.Keys.ToList();

            var batches = await uow.Reports.GetBatchesWithExpiryAsync(HasProductFilter(filter) ? productIds : null);

            var batchProductIds = batches.Select(b => b.ProductId).Distinct().ToList();
            var states = await uow.Ledger.GetStatesAsync(batchProductIds);
            DateTime today = DateTime.Today;

            var rows = new List<StockExpiryRowDto2>();
            foreach (var b in batches)
            {
                if (!productMap.TryGetValue(b.ProductId, out var product)) continue;
                if (!states.TryGetValue(b.ProductId, out var state)) continue;

                // The batch is only "in stock" while its ledger row still has FIFO quantity left.
                var layer = state.Layers.FirstOrDefault(l => l.VendorStockId == b.VendorStockId);
                if (layer == null) continue;

                int days = (b.ExpiryDate.Date - today).Days;
                if (!filter.IncludeZero && days > filter.DaysAhead) continue;

                rows.Add(new StockExpiryRowDto2
                {
                    ProductId = b.ProductId,
                    ProductCode = product.Code,
                    ProductName = product.Name,
                    BatchNumber = b.BatchNumber ?? string.Empty,
                    ExpiryDate = b.ExpiryDate.Date,
                    DaysToExpiry = days,
                    RemainingQuantity = layer.RemainingQuantity,
                    UnitCost = Math.Round(layer.UnitCost, 4),
                    ValueAtRisk = Math.Round(layer.RemainingQuantity * layer.UnitCost, 2),
                    Status = days < 0 ? "Expired" : days <= filter.DaysAhead ? "Expiring soon" : "OK"
                });
            }

            return rows;
        }

        public Task<BaseResponse<PagedResult2<StockExpiryRowDto2>>> GetExpiryAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();

                var rows = await BuildExpiryRowsAsync(uow, filter);
                var page = StockPaging2.ToPage(rows, filter, nameof(StockExpiryRowDto2.ExpiryDate));
                page.Totals["ValueAtRisk"] = rows.Sum(r => r.ValueAtRisk);
                page.Totals["Units"] = rows.Sum(r => (decimal)r.RemainingQuantity);
                return StockResult2.Ok(page);
            }, _logRepo, nameof(GetExpiryAsync), null, filter);
        }

        // ================================================================== reconciliation

        public Task<BaseResponse<PagedResult2<StockReconciliationRowDto2>>> GetReconciliationAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();

                var products = await uow.Reports.GetProductsAsync(filter, false);
                ICollection<int>? ids = HasProductFilter(filter) ? products.Select(p => p.Id).ToList() : null;
                var states = await uow.Ledger.GetStatesAsync(ids);
                var balances = await uow.Ledger.GetBalanceTotalsAsync(ids);
                var transit = await uow.Ledger.GetInTransitAsync(ids);

                var rows = new List<StockReconciliationRowDto2>();
                foreach (var p in products)
                {
                    states.TryGetValue(p.Id, out var state);
                    int onHand = state?.OnHand ?? 0;
                    int other = StockLedgerRules2.Get(balances, p.Id);
                    int inTransit = StockLedgerRules2.Get(transit, p.Id);
                    int defaultQty = StockLedgerRules2.QuantityAtLocation(onHand, other, inTransit, true, 0);
                    int layerRemaining = state?.Layers.Sum(l => l.RemainingQuantity) ?? 0;

                    var issues = new List<string>();
                    if (onHand < 0)
                        issues.Add("Negative stock: more was sold or issued than received.");
                    else if (defaultQty < 0)
                        issues.Add("Quantity allocated to other locations or in transit exceeds the stock on hand.");
                    if (onHand >= 0 && layerRemaining != onHand)
                        issues.Add("Cost layers do not add up to the ledger quantity.");
                    if (onHand > 0 && (state?.HasEstimatedCost ?? false))
                        issues.Add("Some stock has no purchase price; valuation uses an estimate.");

                    if (issues.Count == 0 && !filter.IncludeZero) continue;

                    rows.Add(new StockReconciliationRowDto2
                    {
                        ProductId = p.Id,
                        ProductCode = p.Code,
                        ProductName = p.Name,
                        LedgerOnHand = onHand,
                        DefaultLocationQty = defaultQty,
                        OtherLocationsQty = other,
                        InTransitQty = inTransit,
                        LayerRemaining = layerRemaining,
                        Issue = string.Join(" ", issues)
                    });
                }

                var page = StockPaging2.ToPage(rows, filter, nameof(StockReconciliationRowDto2.ProductName));
                return StockResult2.Ok(page);
            }, _logRepo, nameof(GetReconciliationAsync), null, filter);
        }

        // ================================================================== reorder + lookup

        public Task<BaseResponse<List<StockReorderSuggestionDto2>>> GetReorderSuggestionsAsync()
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();

                var settings = await uow.Master.GetReorderSettingsAsync();
                var productIds = settings.Select(s => s.ProductId).ToList();

                var idSet = new HashSet<int>(productIds);
                var products = (await uow.Reports.GetProductsAsync(new StockQueryFilter2(), false))
                    .Where(p => idSet.Contains(p.Id))
                    .ToDictionary(p => p.Id);

                var states = await uow.Ledger.GetStatesAsync(productIds);
                var onOrder = await uow.Receiving.GetOnOrderQuantitiesAsync(productIds);
                var vendorNames = await uow.Master.GetSupplierNamesAsync();

                var result = new List<StockReorderSuggestionDto2>();
                foreach (var s in settings)
                {
                    if (!products.TryGetValue(s.ProductId, out var p)) continue;
                    states.TryGetValue(s.ProductId, out var state);
                    int onHand = state?.OnHand ?? 0;
                    int ordered = Math.Max(0, StockLedgerRules2.Get(onOrder, s.ProductId));

                    // Enough is on hand or already on order.
                    if (onHand + ordered > s.ReorderLevel) continue;

                    int target = s.MaxLevel ?? (s.ReorderLevel + Math.Max(s.ReorderQuantity, 1));
                    int suggested = s.ReorderQuantity > 0 ? s.ReorderQuantity : Math.Max(1, target - onHand - ordered);

                    string? vendorName = null;
                    if (s.PreferredVendorId.HasValue) vendorNames.TryGetValue(s.PreferredVendorId.Value, out vendorName);

                    result.Add(new StockReorderSuggestionDto2
                    {
                        ProductId = s.ProductId,
                        ProductCode = p.Code,
                        ProductName = p.Name,
                        OnHand = onHand,
                        ReorderLevel = s.ReorderLevel,
                        ReorderQuantity = s.ReorderQuantity,
                        MaxLevel = s.MaxLevel ?? 0,
                        OnOrder = ordered,
                        SuggestedQuantity = suggested,
                        LastUnitCost = Math.Round(state?.LastUnitCost ?? 0m, 4),
                        PreferredVendorId = s.PreferredVendorId,
                        PreferredVendorName = vendorName ?? string.Empty
                    });
                }

                return StockResult2.Ok(result.OrderBy(r => r.OnHand - r.ReorderLevel).ThenBy(r => r.ProductName).ToList());
            }, _logRepo, nameof(GetReorderSuggestionsAsync));
        }

        public Task<BaseResponse<List<ProductLookupDto2>>> GetProductLookupAsync()
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();

                var products = await uow.Reports.GetProductsAsync(new StockQueryFilter2(), false);
                var states = await uow.Ledger.GetStatesAsync();

                var list = products.OrderBy(p => p.Name).Select(p =>
                {
                    states.TryGetValue(p.Id, out var state);
                    return new ProductLookupDto2
                    {
                        ProductId = p.Id,
                        ProductCode = p.Code,
                        ProductName = p.Name,
                        CategoryName = p.Category,
                        OnHand = state?.OnHand ?? 0,
                        LastUnitCost = Math.Round(state?.LastUnitCost ?? 0m, 4)
                    };
                }).ToList();

                return StockResult2.Ok(list);
            }, _logRepo, nameof(GetProductLookupAsync));
        }

        // ================================================================== dashboard

        public Task<BaseResponse<StockDashboardDto2>> GetDashboardAsync()
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();

                var rows = await BuildOverviewRowsAsync(uow, new StockQueryFilter2 { IncludeZero = true });
                var lowRows = rows.Where(r => r.ReorderLevel > 0 && r.OnHand <= r.ReorderLevel).ToList();

                var dto = new StockDashboardDto2
                {
                    ProductCount = rows.Count,
                    InStockCount = rows.Count(r => r.OnHand > 0),
                    LowStockCount = lowRows.Count,
                    OutOfStockCount = rows.Count(r => r.OnHand == 0),
                    NegativeStockCount = rows.Count(r => r.OnHand < 0),
                    TotalUnitsOnHand = rows.Where(r => r.OnHand > 0).Sum(r => (long)r.OnHand),
                    TotalStockValue = rows.Sum(r => r.StockValue),
                    LowStock = lowRows.OrderBy(r => r.OnHand).ThenBy(r => r.ProductName).Take(10).ToList(),
                    ValueByCategory = rows.Where(r => r.StockValue > 0)
                        .GroupBy(r => r.CategoryName)
                        .ToDictionary(g => g.Key, g => g.Sum(r => r.StockValue))
                };

                var expiry = await BuildExpiryRowsAsync(uow, new StockQueryFilter2 { DaysAhead = StockConstants2.ExpiryWarningDays });
                dto.ExpiredBatches = expiry.Count(e => e.Status == "Expired");
                dto.ExpiringSoonBatches = expiry.Count(e => e.Status == "Expiring soon");
                dto.ExpiringBatches = expiry.OrderBy(e => e.ExpiryDate).Take(10).ToList();

                dto.OpenPurchaseOrders = await uow.Receiving.CountOpenPurchaseOrdersAsync();
                dto.InTransitTransfers = await uow.Transfers.CountInTransitTransfersAsync();

                var recent = await BuildMovementRowsAsync(uow, new StockQueryFilter2 { FromDate = DateTime.Today.AddDays(-30) });
                dto.MovementsToday = recent.Count(r => r.MovementDate >= DateTime.Today && !r.IsReversed);
                dto.RecentMovements = recent.OrderByDescending(r => r.MovementDate).Take(10).ToList();

                return StockResult2.Ok(dto);
            }, _logRepo, nameof(GetDashboardAsync));
        }
    }
}
