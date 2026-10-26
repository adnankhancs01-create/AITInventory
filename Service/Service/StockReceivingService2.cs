using Common;
using Common.Stock2;
using Domain.Entities;
using Domain.IRepositories;
using Domain.Stock2;
using Domain.Users2;
using Service.IService;

namespace Service.Service
{
    /// <summary>Purchase orders and goods receipts. A receipt posts positive Stock rows so every existing page sees the new stock.</summary>
    public class StockReceivingService2 : IStockReceivingService2
    {
        private readonly IStockUnitOfWorkFactory2 _uowFactory;
        private readonly ILogRepository _logRepo;
        private readonly ICurrentUser2 _currentUser;

        public StockReceivingService2(IStockUnitOfWorkFactory2 uowFactory, ILogRepository logRepo, ICurrentUser2 currentUser)
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

        // ================================================================== purchase orders

        public Task<BaseResponse<int>> CreatePurchaseOrderAsync(PurchaseOrderRequestDto2 request)
        {
            int userId = StockAccess2.ActingUserId(_currentUser, request.UserId);

            return StockResult2.RunAsync(async () =>
            {
                var denied = StockAccess2.Check<int>(_currentUser, ScreenCatalog2.StockPurchaseOrders);
                if (denied != null) return denied;

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

                await using var uow = _uowFactory.Create();

                var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
                var names = await uow.Ledger.GetActiveProductNamesAsync(productIds);
                foreach (var id in productIds.Where(pid => !names.ContainsKey(pid)))
                    errors.Add($"Product {id} was not found or is inactive.");

                string? supplierName = Clean(request.SupplierName, 150);
                if (request.VendorId.HasValue)
                {
                    var vendor = await uow.Master.FindSupplierAsync(request.VendorId.Value);
                    if (vendor == null) errors.Add("Supplier not found.");
                    else supplierName ??= vendor.Name;
                }
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                await uow.BeginTransactionAsync();

                string prefix = StockLedgerRules2.DocumentPrefix("PO", date);
                int existing = await uow.Receiving.CountPurchaseOrdersWithPrefixAsync(prefix);

                var po = new PurchaseOrder2
                {
                    PoNumber = StockLedgerRules2.BuildDocumentNumber("PO", date, existing),
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

                uow.Receiving.AddPurchaseOrder(po);
                await uow.SaveChangesAsync();
                await uow.CommitAsync();

                return StockResult2.Ok(po.Id, $"Purchase order {po.PoNumber} created.");
            }, _logRepo, nameof(CreatePurchaseOrderAsync), userId, request);
        }

        public Task<BaseResponse<bool>> CancelPurchaseOrderAsync(int purchaseOrderId, int? userId)
        {
            return StockResult2.RunAsync(async () =>
            {
                var denied = StockAccess2.Check<bool>(_currentUser, ScreenCatalog2.StockPurchaseOrders);
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();

                var po = await uow.Receiving.GetPurchaseOrderWithLinesAsync(purchaseOrderId);
                if (po == null) return StockResult2.Fail<bool>("Purchase order not found.");
                if (po.Status != StockConstants2.StatusOpen)
                    return StockResult2.Fail<bool>($"Only open purchase orders can be cancelled (this one is {po.Status}).");
                if (po.Lines.Any(l => l.ReceivedQuantity > 0))
                    return StockResult2.Fail<bool>("Goods were already received against this order.");

                po.Status = StockConstants2.StatusCancelled;
                await uow.SaveChangesAsync();
                return StockResult2.Ok(true, $"Purchase order {po.PoNumber} cancelled.");
            }, _logRepo, nameof(CancelPurchaseOrderAsync), userId, new { purchaseOrderId });
        }

        public Task<BaseResponse<PagedResult2<DocumentRowDto2>>> GetPurchaseOrdersAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();
                var rows = await uow.Receiving.GetPurchaseOrderRowsAsync(filter);

                var page = StockPaging2.ToPage(rows, filter, nameof(DocumentRowDto2.Date), true);
                page.Totals["TotalValue"] = rows.Sum(r => r.TotalValue);
                return StockResult2.Ok(page);
            }, _logRepo, nameof(GetPurchaseOrdersAsync), null, filter);
        }

        public Task<BaseResponse<List<DocumentLineDto2>>> GetPurchaseOrderLinesAsync(int purchaseOrderId)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();
                var lines = await uow.Receiving.GetPurchaseOrderLinesAsync(purchaseOrderId);

                foreach (var l in lines)
                {
                    l.Note = l.ProcessedQuantity >= l.Quantity ? "Fully received"
                        : l.ProcessedQuantity > 0 ? "Partly received" : "Not received";
                }

                return StockResult2.Ok(lines);
            }, _logRepo, nameof(GetPurchaseOrderLinesAsync), null, new { purchaseOrderId });
        }

        // ================================================================== receipts

        public Task<BaseResponse<int>> PostReceiptAsync(ReceiptRequestDto2 request)
        {
            int userId = StockAccess2.ActingUserId(_currentUser, request.UserId);

            return StockResult2.RunAsync(async () =>
            {
                var denied = StockAccess2.Check<int>(_currentUser, ScreenCatalog2.StockReceiving);
                if (denied != null) return denied;

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

                await using var uow = _uowFactory.Create();
                await uow.Ledger.EnsureDefaultLocationAsync();

                var location = await uow.Ledger.GetLocationAsync(request.LocationId, true);
                if (location == null) return StockResult2.Fail<int>("Location not found or inactive.");

                var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
                var names = await uow.Ledger.GetActiveProductNamesAsync(productIds);
                foreach (var id in productIds.Where(pid => !names.ContainsKey(pid)))
                    errors.Add($"Product {id} was not found or is inactive.");
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                await uow.BeginTransactionAsync();

                // ---- optional purchase order
                PurchaseOrder2? po = null;
                if (request.PurchaseOrderId.HasValue)
                {
                    po = await uow.Receiving.GetPurchaseOrderWithLinesAsync(request.PurchaseOrderId.Value);
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
                                && x.OrderedQuantity - x.ReceivedQuantity - StockLedgerRules2.Get(consumed, x.Id) > 0);
                        }

                        if (poLine == null)
                        {
                            errors.Add($"'{names[line.ProductId]}' is not on purchase order {po.PoNumber} or is already fully received.");
                            continue;
                        }

                        consumed[poLine.Id] = StockLedgerRules2.Get(consumed, poLine.Id) + line.Quantity;
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
                    var vendor = await uow.Master.FindSupplierAsync(vendorId.Value);
                    if (vendor == null) return StockResult2.Fail<int>("Supplier not found.");
                    supplierName ??= vendor.Name;
                }

                // ---- header
                string prefix = StockLedgerRules2.DocumentPrefix("GRN", date);
                int existing = await uow.Receiving.CountReceiptsWithPrefixAsync(prefix);

                var receipt = new StockReceipt2
                {
                    ReceiptNumber = StockLedgerRules2.BuildDocumentNumber("GRN", date, existing),
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
                    var row = StockLedgerRules2.NewStockInRow(req.ProductId, req.Quantity, total, vendorId, userId);
                    uow.Ledger.AddLedgerRow(row);

                    uow.Ledger.AddBatch(new StockBatch2
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

                    await uow.Ledger.ChangeBalanceAsync(req.ProductId, location, req.Quantity);
                    if (poLine != null) poLine.ReceivedQuantity += req.Quantity;
                    created.Add((req, row));
                }

                uow.Receiving.AddReceipt(receipt);

                if (po != null)
                {
                    po.Status = po.Lines.All(x => x.ReceivedQuantity >= x.OrderedQuantity)
                        ? StockConstants2.StatusReceived
                        : StockConstants2.StatusPartiallyReceived;
                    // Touch the header so its RowVersion is checked (blocks two users receiving the same order at once).
                    uow.Receiving.MarkPurchaseOrderModified(po);
                }

                await uow.SaveChangesAsync(); // receipt now has its Id

                foreach (var (req, row) in created)
                {
                    uow.Ledger.AddMovement(StockLedgerRules2.NewMovement(
                        req.ProductId, location.Id, StockConstants2.MoveReceipt, req.Quantity, req.UnitCost,
                        StockConstants2.DocReceipt, receipt.Id, receipt.ReceiptNumber, row,
                        Clean(supplierName, 100), Clean(req.BatchNumber, 500), userId));
                }

                await uow.SaveChangesAsync();
                await uow.CommitAsync();

                return StockResult2.Ok(receipt.Id, $"Receipt {receipt.ReceiptNumber} posted.");
            }, _logRepo, nameof(PostReceiptAsync), userId, request);
        }

        public Task<BaseResponse<bool>> CancelReceiptAsync(int receiptId, int? userId)
        {
            int actor = StockAccess2.ActingUserId(_currentUser, userId);

            return StockResult2.RunAsync(async () =>
            {
                var denied = StockAccess2.Check<bool>(_currentUser, ScreenCatalog2.StockReceiving);
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                await uow.Ledger.EnsureDefaultLocationAsync();
                await uow.BeginTransactionAsync();

                var receipt = await uow.Receiving.GetReceiptForCancelAsync(receiptId);

                if (receipt == null) return StockResult2.Fail<bool>("Receipt not found.");
                if (receipt.Status != StockConstants2.StatusPosted)
                    return StockResult2.Fail<bool>($"Receipt {receipt.ReceiptNumber} is already {receipt.Status}.");

                var location = receipt.Location!;
                var errors = new List<string>();

                // ---- the stock must still be there: neither sold/issued nor moved to another location
                var perProduct = receipt.Lines.GroupBy(l => l.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
                var productIds = perProduct.Keys.ToList();
                var names = await uow.Ledger.GetProductNamesAsync(productIds);

                foreach (var pair in perProduct)
                {
                    int atLocation = await uow.Ledger.GetQuantityAtLocationAsync(pair.Key, location);
                    if (atLocation < pair.Value)
                        errors.Add($"'{names[pair.Key]}': {pair.Value} were received but only {atLocation} remain at {location.Name}. Sold, issued or transferred stock cannot be cancelled.");
                }
                if (errors.Count > 0) return StockResult2.Fail<bool>(errors);

                // ---- deactivate ledger rows (the same convention the existing revert uses)
                var ledgerIds = receipt.Lines.Where(l => l.VendorStockId.HasValue).Select(l => l.VendorStockId!.Value).ToList();
                var ledgerRows = await uow.Ledger.GetLedgerRowsAsync(ledgerIds);
                foreach (var row in ledgerRows)
                {
                    row.IsActive = false;
                    row.ModifiedOn = DateTime.Now;
                    row.ModifiedBy = actor;
                }

                foreach (var line in receipt.Lines)
                {
                    await uow.Ledger.ChangeBalanceAsync(line.ProductId, location, -line.Quantity);
                    if (line.PurchaseOrderLine != null)
                        line.PurchaseOrderLine.ReceivedQuantity = Math.Max(0, line.PurchaseOrderLine.ReceivedQuantity - line.Quantity);
                }

                if (receipt.PurchaseOrderId.HasValue)
                {
                    var po = await uow.Receiving.GetPurchaseOrderWithLinesAsync(receipt.PurchaseOrderId.Value);
                    if (po != null && po.Status != StockConstants2.StatusCancelled)
                    {
                        po.Status = po.Lines.All(x => x.ReceivedQuantity >= x.OrderedQuantity) ? StockConstants2.StatusReceived
                            : po.Lines.Any(x => x.ReceivedQuantity > 0) ? StockConstants2.StatusPartiallyReceived
                            : StockConstants2.StatusOpen;
                        uow.Receiving.MarkPurchaseOrderModified(po);
                    }
                }

                receipt.Status = StockConstants2.StatusCancelled;
                receipt.CancelledOn = DateTime.Now;
                receipt.CancelledBy = actor;

                await uow.SaveChangesAsync();
                await uow.CommitAsync();
                return StockResult2.Ok(true, $"Receipt {receipt.ReceiptNumber} cancelled and its stock removed.");
            }, _logRepo, nameof(CancelReceiptAsync), actor, new { receiptId });
        }

        public Task<BaseResponse<PagedResult2<DocumentRowDto2>>> GetReceiptsAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();
                var rows = await uow.Receiving.GetReceiptRowsAsync(filter);

                var page = StockPaging2.ToPage(rows, filter, nameof(DocumentRowDto2.Date), true);
                page.Totals["TotalValue"] = rows.Where(r => r.Status == StockConstants2.StatusPosted).Sum(r => r.TotalValue);
                return StockResult2.Ok(page);
            }, _logRepo, nameof(GetReceiptsAsync), null, filter);
        }

        public Task<BaseResponse<List<DocumentLineDto2>>> GetReceiptLinesAsync(int receiptId)
        {
            return StockResult2.RunAsync(async () =>
            {
                await using var uow = _uowFactory.Create();
                var lines = await uow.Receiving.GetReceiptLinesAsync(receiptId);
                return StockResult2.Ok(lines);
            }, _logRepo, nameof(GetReceiptLinesAsync), null, new { receiptId });
        }
    }
}
