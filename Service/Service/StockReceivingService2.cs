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
    /// <summary>Purchase orders and goods receipts. A receipt posts positive VendorStock rows so every existing page sees the new stock.</summary>
    public class StockReceivingService2 : IStockReceivingService2
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogRepository _logRepo;

        public StockReceivingService2(IServiceScopeFactory scopeFactory, ILogRepository logRepo)
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

        // ================================================================== purchase orders

        public Task<BaseResponse<int>> CreatePurchaseOrderAsync(PurchaseOrderRequestDto2 request)
        {
            int userId = request.UserId ?? StockConstants2.SystemUserId;

            return StockResult2.RunAsync(async () =>
            {
                DateTime date = request.DocumentDate ?? DateTime.Now;
                var errors = new List<string>();

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
                        if (line.UnitCost < 0) errors.Add(n + "unit cost cannot be negative.");
                    }

                    if (request.Lines.GroupBy(l => l.ProductId).Any(g => g.Count() > 1))
                        errors.Add("A product can appear only once on a purchase order.");
                }

                if (request.ExpectedDate.HasValue && request.ExpectedDate.Value.Date < date.Date)
                    errors.Add("Expected date cannot be before the order date.");
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
                var names = await StockLedgerHelper2.LoadActiveProductNamesAsync(db, productIds);
                foreach (var id in productIds.Where(pid => !names.ContainsKey(pid)))
                    errors.Add($"Product {id} was not found or is inactive.");

                string? supplierName = Clean(request.SupplierName, 150);
                if (request.VendorId.HasValue)
                {
                    var vendor = await db.Vendor.AsNoTracking().FirstOrDefaultAsync(v => v.Id == request.VendorId.Value);
                    if (vendor == null) errors.Add("Supplier not found.");
                    else supplierName ??= vendor.Name;
                }
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                await using var tx = await db.Database.BeginTransactionAsync();

                string prefix = $"PO-{date:yyyyMMdd}-";
                int existing = await db.PurchaseOrders2.CountAsync(p => p.PoNumber.StartsWith(prefix));

                var po = new PurchaseOrder2
                {
                    PoNumber = StockLedgerHelper2.BuildDocumentNumber("PO", date, existing),
                    PoDate = date,
                    ExpectedDate = request.ExpectedDate?.Date,
                    VendorId = request.VendorId,
                    SupplierName = supplierName,
                    Status = StockConstants2.StatusOpen,
                    Remarks = Clean(request.Remarks, 500),
                    CreatedBy = userId,
                    CreatedOn = DateTime.Now
                };

                foreach (var line in request.Lines)
                {
                    po.Lines.Add(new PurchaseOrderLine2
                    {
                        ProductId = line.ProductId,
                        OrderedQuantity = line.Quantity,
                        ReceivedQuantity = 0,
                        UnitCost = line.UnitCost
                    });
                }

                db.PurchaseOrders2.Add(po);
                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return StockResult2.Ok(po.Id, $"Purchase order {po.PoNumber} created.");
            }, _logRepo, nameof(CreatePurchaseOrderAsync), userId, request);
        }

        public Task<BaseResponse<bool>> CancelPurchaseOrderAsync(int purchaseOrderId, int? userId)
        {
            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var po = await db.PurchaseOrders2.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == purchaseOrderId);
                if (po == null) return StockResult2.Fail<bool>("Purchase order not found.");
                if (po.Status != StockConstants2.StatusOpen)
                    return StockResult2.Fail<bool>($"Only open purchase orders can be cancelled (this one is {po.Status}).");
                if (po.Lines.Any(l => l.ReceivedQuantity > 0))
                    return StockResult2.Fail<bool>("Goods were already received against this order.");

                po.Status = StockConstants2.StatusCancelled;
                await db.SaveChangesAsync();
                return StockResult2.Ok(true, $"Purchase order {po.PoNumber} cancelled.");
            }, _logRepo, nameof(CancelPurchaseOrderAsync), userId, new { purchaseOrderId });
        }

        public Task<BaseResponse<PagedResult2<DocumentRowDto2>>> GetPurchaseOrdersAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var query = db.PurchaseOrders2.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(p => p.Status == filter.Status);
                if (filter.FromDate.HasValue) { var f = filter.FromDate.Value.Date; query = query.Where(p => p.PoDate >= f); }
                if (filter.ToDate.HasValue) { var t = filter.ToDate.Value.Date.AddDays(1); query = query.Where(p => p.PoDate < t); }
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    string s = filter.Search.Trim();
                    query = query.Where(p => p.PoNumber.Contains(s) || (p.SupplierName != null && p.SupplierName.Contains(s)));
                }

                var rows = await query.Select(p => new DocumentRowDto2
                {
                    Id = p.Id,
                    Number = p.PoNumber,
                    Date = p.PoDate,
                    Status = p.Status,
                    Party = p.SupplierName ?? string.Empty,
                    Reason = string.Empty,
                    LineCount = p.Lines.Count,
                    TotalQuantity = p.Lines.Sum(l => l.OrderedQuantity),
                    TotalValue = p.Lines.Sum(l => l.OrderedQuantity * l.UnitCost),
                    Remarks = p.Remarks ?? string.Empty
                }).ToListAsync();

                var page = StockPaging2.ToPage(rows, filter, nameof(DocumentRowDto2.Date), true);
                page.Totals["TotalValue"] = rows.Sum(r => r.TotalValue);
                return StockResult2.Ok(page);
            }, _logRepo, nameof(GetPurchaseOrdersAsync), null, filter);
        }

        public Task<BaseResponse<List<DocumentLineDto2>>> GetPurchaseOrderLinesAsync(int purchaseOrderId)
        {
            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var lines = await db.PurchaseOrderLines2.AsNoTracking()
                    .Where(l => l.PurchaseOrderId == purchaseOrderId)
                    .OrderBy(l => l.Id)
                    .Select(l => new DocumentLineDto2
                    {
                        LineId = l.Id,
                        ProductId = l.ProductId,
                        ProductCode = l.Product!.ProductCode,
                        ProductName = l.Product!.Name ?? string.Empty,
                        Quantity = l.OrderedQuantity,
                        ProcessedQuantity = l.ReceivedQuantity,
                        UnitCost = l.UnitCost,
                        Note = l.ReceivedQuantity >= l.OrderedQuantity ? "Fully received"
                            : l.ReceivedQuantity > 0 ? "Partly received" : "Not received"
                    }).ToListAsync();

                return StockResult2.Ok(lines);
            }, _logRepo, nameof(GetPurchaseOrderLinesAsync), null, new { purchaseOrderId });
        }

        // ================================================================== receipts

        public Task<BaseResponse<int>> PostReceiptAsync(ReceiptRequestDto2 request)
        {
            int userId = request.UserId ?? StockConstants2.SystemUserId;

            return StockResult2.RunAsync(async () =>
            {
                DateTime date = request.DocumentDate ?? DateTime.Now;
                var errors = new List<string>();

                if (request.LocationId <= 0) errors.Add("Choose a receiving location.");
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
                        if (line.UnitCost <= 0) errors.Add(n + "unit cost must be greater than zero.");
                        if ((line.BatchNumber?.Trim().Length ?? 0) > 50) errors.Add(n + "batch number can be at most 50 characters.");
                        if (line.ExpiryDate.HasValue && line.ExpiryDate.Value.Date <= date.Date)
                            errors.Add(n + "expiry date must be after the receipt date.");
                    }
                }
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                await StockLedgerHelper2.EnsureDefaultLocationAsync(db);

                var location = await db.StockLocations2.FirstOrDefaultAsync(l => l.Id == request.LocationId && l.IsActive);
                if (location == null) return StockResult2.Fail<int>("Location not found or inactive.");

                var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
                var names = await StockLedgerHelper2.LoadActiveProductNamesAsync(db, productIds);
                foreach (var id in productIds.Where(pid => !names.ContainsKey(pid)))
                    errors.Add($"Product {id} was not found or is inactive.");
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                await using var tx = await db.Database.BeginTransactionAsync();

                // ---- optional purchase order
                PurchaseOrder2? po = null;
                if (request.PurchaseOrderId.HasValue)
                {
                    po = await db.PurchaseOrders2.Include(p => p.Lines)
                        .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId.Value);
                    if (po == null) return StockResult2.Fail<int>("Purchase order not found.");
                    if (po.Status != StockConstants2.StatusOpen && po.Status != StockConstants2.StatusPartiallyReceived)
                        return StockResult2.Fail<int>($"Purchase order {po.PoNumber} is {po.Status} and cannot be received.");
                }

                // ---- match every receipt line to a purchase order line and block over-receipt
                var matches = new List<(DocumentLineRequestDto2 Request, PurchaseOrderLine2? PoLine)>();
                var consumed = new Dictionary<int, int>();
                foreach (var line in request.Lines)
                {
                    PurchaseOrderLine2? poLine = null;
                    if (po != null)
                    {
                        if (line.PurchaseOrderLineId.HasValue)
                        {
                            poLine = po.Lines.FirstOrDefault(x => x.Id == line.PurchaseOrderLineId.Value && x.ProductId == line.ProductId);
                        }
                        else
                        {
                            poLine = po.Lines.FirstOrDefault(x => x.ProductId == line.ProductId
                                && x.OrderedQuantity - x.ReceivedQuantity - StockLedgerHelper2.Get(consumed, x.Id) > 0);
                        }

                        if (poLine == null)
                        {
                            errors.Add($"'{names[line.ProductId]}' is not on purchase order {po.PoNumber} or is already fully received.");
                            continue;
                        }

                        consumed[poLine.Id] = StockLedgerHelper2.Get(consumed, poLine.Id) + line.Quantity;
                    }

                    matches.Add((line, poLine));
                }

                if (po != null)
                {
                    foreach (var pair in consumed)
                    {
                        var poLine = po.Lines.First(x => x.Id == pair.Key);
                        int outstanding = poLine.OrderedQuantity - poLine.ReceivedQuantity;
                        if (pair.Value > outstanding)
                            errors.Add($"'{names[poLine.ProductId]}': receiving {pair.Value} but only {outstanding} is outstanding on {po.PoNumber}.");
                    }
                }
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                // ---- supplier
                int? vendorId = request.VendorId ?? po?.VendorId;
                string? supplierName = Clean(request.SupplierName, 150) ?? po?.SupplierName;
                if (vendorId.HasValue)
                {
                    var vendor = await db.Vendor.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vendorId.Value);
                    if (vendor == null) return StockResult2.Fail<int>("Supplier not found.");
                    supplierName ??= vendor.Name;
                }

                // ---- header
                string prefix = $"GRN-{date:yyyyMMdd}-";
                int existing = await db.StockReceipts2.CountAsync(r => r.ReceiptNumber.StartsWith(prefix));

                var receipt = new StockReceipt2
                {
                    ReceiptNumber = StockLedgerHelper2.BuildDocumentNumber("GRN", date, existing),
                    ReceiptDate = date,
                    VendorId = vendorId,
                    SupplierName = supplierName,
                    SupplierInvoiceNo = Clean(request.SupplierInvoiceNo, 50),
                    PurchaseOrderId = po?.Id,
                    LocationId = location.Id,
                    Status = StockConstants2.StatusPosted,
                    Remarks = Clean(request.Remarks, 500),
                    CreatedBy = userId,
                    CreatedOn = DateTime.Now
                };

                // ---- lines: one positive ledger row + one batch row per line
                var created = new List<(DocumentLineRequestDto2 Request, Stock Row)>();
                foreach (var (req, poLine) in matches)
                {
                    decimal total = Math.Round(req.Quantity * req.UnitCost, 2);
                    var row = StockLedgerHelper2.NewStockInRow(req.ProductId, req.Quantity, total, vendorId, userId);
                    db.Stock.Add(row);

                    db.StockBatches2.Add(new StockBatch2
                    {
                        ProductId = req.ProductId,
                        VendorStock = row,
                        BatchNumber = Clean(req.BatchNumber, 50),
                        ExpiryDate = req.ExpiryDate?.Date,
                        ReceivedQuantity = req.Quantity,
                        UnitCost = req.UnitCost,
                        LocationId = location.Id,
                        CreatedOn = row.CreatedOn ?? DateTime.Now
                    });

                    receipt.Lines.Add(new StockReceiptLine2
                    {
                        ProductId = req.ProductId,
                        Quantity = req.Quantity,
                        UnitCost = req.UnitCost,
                        BatchNumber = Clean(req.BatchNumber, 50),
                        ExpiryDate = req.ExpiryDate?.Date,
                        PurchaseOrderLine = poLine,
                        VendorStock = row
                    });

                    await StockLedgerHelper2.ChangeBalanceAsync(db, req.ProductId, location, req.Quantity);
                    if (poLine != null) poLine.ReceivedQuantity += req.Quantity;
                    created.Add((req, row));
                }

                db.StockReceipts2.Add(receipt);

                if (po != null)
                {
                    po.Status = po.Lines.All(x => x.ReceivedQuantity >= x.OrderedQuantity)
                        ? StockConstants2.StatusReceived
                        : StockConstants2.StatusPartiallyReceived;
                    // Touch the header so its RowVersion is checked (blocks two users receiving the same order at once).
                    db.Entry(po).Property(x => x.Status).IsModified = true;
                }

                await db.SaveChangesAsync(); // receipt now has its Id

                foreach (var (req, row) in created)
                {
                    db.StockMovements2.Add(StockLedgerHelper2.NewMovement(
                        req.ProductId, location.Id, StockConstants2.MoveReceipt, req.Quantity, req.UnitCost,
                        StockConstants2.DocReceipt, receipt.Id, receipt.ReceiptNumber, row,
                        Clean(supplierName, 100), Clean(req.BatchNumber, 500), userId));
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return StockResult2.Ok(receipt.Id, $"Receipt {receipt.ReceiptNumber} posted.");
            }, _logRepo, nameof(PostReceiptAsync), userId, request);
        }

        public Task<BaseResponse<bool>> CancelReceiptAsync(int receiptId, int? userId)
        {
            int actor = userId ?? StockConstants2.SystemUserId;

            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                await StockLedgerHelper2.EnsureDefaultLocationAsync(db);

                await using var tx = await db.Database.BeginTransactionAsync();

                var receipt = await db.StockReceipts2
                    .Include(r => r.Lines).ThenInclude(l => l.PurchaseOrderLine)
                    .Include(r => r.Location)
                    .FirstOrDefaultAsync(r => r.Id == receiptId);

                if (receipt == null) return StockResult2.Fail<bool>("Receipt not found.");
                if (receipt.Status != StockConstants2.StatusPosted)
                    return StockResult2.Fail<bool>($"Receipt {receipt.ReceiptNumber} is already {receipt.Status}.");

                var location = receipt.Location!;
                var errors = new List<string>();

                // ---- the stock must still be there: neither sold/issued nor moved to another location
                var perProduct = receipt.Lines.GroupBy(l => l.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
                var productIds = perProduct.Keys.ToList();
                var names = await db.Products.AsNoTracking().Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p.Name ?? p.ProductCode);

                foreach (var pair in perProduct)
                {
                    int atLocation = await StockLedgerHelper2.GetQuantityAtLocationAsync(db, pair.Key, location);
                    if (atLocation < pair.Value)
                        errors.Add($"'{names[pair.Key]}': {pair.Value} were received but only {atLocation} remain at {location.Name}. Sold, issued or transferred stock cannot be cancelled.");
                }
                if (errors.Count > 0) return StockResult2.Fail<bool>(errors);

                // ---- deactivate ledger rows (the same convention the existing revert uses)
                var ledgerIds = receipt.Lines.Where(l => l.VendorStockId.HasValue).Select(l => l.VendorStockId!.Value).ToList();
                var ledgerRows = await db.  Stock.Where(v => ledgerIds.Contains(v.Id)).ToListAsync();
                foreach (var row in ledgerRows)
                {
                    row.IsActive = false;
                    row.ModifiedOn = DateTime.Now;
                    row.ModifiedBy = actor;
                }

                foreach (var line in receipt.Lines)
                {
                    await StockLedgerHelper2.ChangeBalanceAsync(db, line.ProductId, location, -line.Quantity);
                    if (line.PurchaseOrderLine != null)
                        line.PurchaseOrderLine.ReceivedQuantity = Math.Max(0, line.PurchaseOrderLine.ReceivedQuantity - line.Quantity);
                }

                if (receipt.PurchaseOrderId.HasValue)
                {
                    var po = await db.PurchaseOrders2.Include(p => p.Lines)
                        .FirstOrDefaultAsync(p => p.Id == receipt.PurchaseOrderId.Value);
                    if (po != null && po.Status != StockConstants2.StatusCancelled)
                    {
                        po.Status = po.Lines.All(x => x.ReceivedQuantity >= x.OrderedQuantity) ? StockConstants2.StatusReceived
                            : po.Lines.Any(x => x.ReceivedQuantity > 0) ? StockConstants2.StatusPartiallyReceived
                            : StockConstants2.StatusOpen;
                        db.Entry(po).Property(x => x.Status).IsModified = true;
                    }
                }

                receipt.Status = StockConstants2.StatusCancelled;
                receipt.CancelledOn = DateTime.Now;
                receipt.CancelledBy = actor;

                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return StockResult2.Ok(true, $"Receipt {receipt.ReceiptNumber} cancelled and its stock removed.");
            }, _logRepo, nameof(CancelReceiptAsync), actor, new { receiptId });
        }

        public Task<BaseResponse<PagedResult2<DocumentRowDto2>>> GetReceiptsAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var query = db.StockReceipts2.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(r => r.Status == filter.Status);
                if (filter.LocationId.HasValue) query = query.Where(r => r.LocationId == filter.LocationId.Value);
                if (filter.FromDate.HasValue) { var f = filter.FromDate.Value.Date; query = query.Where(r => r.ReceiptDate >= f); }
                if (filter.ToDate.HasValue) { var t = filter.ToDate.Value.Date.AddDays(1); query = query.Where(r => r.ReceiptDate < t); }
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    string s = filter.Search.Trim();
                    query = query.Where(r => r.ReceiptNumber.Contains(s)
                        || (r.SupplierName != null && r.SupplierName.Contains(s))
                        || (r.SupplierInvoiceNo != null && r.SupplierInvoiceNo.Contains(s)));
                }

                var rows = await query.Select(r => new DocumentRowDto2
                {
                    Id = r.Id,
                    Number = r.ReceiptNumber,
                    Date = r.ReceiptDate,
                    Status = r.Status,
                    Party = r.SupplierName ?? string.Empty,
                    Reason = r.SupplierInvoiceNo ?? string.Empty,
                    LineCount = r.Lines.Count,
                    TotalQuantity = r.Lines.Sum(l => l.Quantity),
                    TotalValue = r.Lines.Sum(l => l.Quantity * l.UnitCost),
                    Remarks = r.Remarks ?? string.Empty
                }).ToListAsync();

                var page = StockPaging2.ToPage(rows, filter, nameof(DocumentRowDto2.Date), true);
                page.Totals["TotalValue"] = rows.Where(r => r.Status == StockConstants2.StatusPosted).Sum(r => r.TotalValue);
                return StockResult2.Ok(page);
            }, _logRepo, nameof(GetReceiptsAsync), null, filter);
        }

        public Task<BaseResponse<List<DocumentLineDto2>>> GetReceiptLinesAsync(int receiptId)
        {
            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var lines = await db.StockReceiptLines2.AsNoTracking()
                    .Where(l => l.ReceiptId == receiptId)
                    .OrderBy(l => l.Id)
                    .Select(l => new DocumentLineDto2
                    {
                        LineId = l.Id,
                        ProductId = l.ProductId,
                        ProductCode = l.Product!.ProductCode,
                        ProductName = l.Product!.Name ?? string.Empty,
                        Quantity = l.Quantity,
                        UnitCost = l.UnitCost,
                        BatchNumber = l.BatchNumber ?? string.Empty,
                        ExpiryDate = l.ExpiryDate
                    }).ToListAsync();

                return StockResult2.Ok(lines);
            }, _logRepo, nameof(GetReceiptLinesAsync), null, new { receiptId });
        }
    }
}
