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
    /// Transfers between locations with in-transit tracking. A transfer is dispatched when created (status InTransit);
    /// the quantity leaves the source immediately and arrives at the destination when the transfer is received.
    /// The total on-hand quantity in the VendorStock ledger never changes.
    /// </summary>
    public class StockTransferService2 : IStockTransferService2
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogRepository _logRepo;

        public StockTransferService2(IServiceScopeFactory scopeFactory, ILogRepository logRepo)
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

        public Task<BaseResponse<int>> CreateTransferAsync(TransferRequestDto2 request)
        {
            int userId = request.UserId ?? StockConstants2.SystemUserId;

            return StockResult2.RunAsync(async () =>
            {
                DateTime date = request.DocumentDate ?? DateTime.Now;
                var errors = new List<string>();

                if (request.FromLocationId <= 0) errors.Add("Choose the source location.");
                if (request.ToLocationId <= 0) errors.Add("Choose the destination location.");
                if (request.FromLocationId > 0 && request.FromLocationId == request.ToLocationId)
                    errors.Add("Source and destination must be different locations.");
                if (date > DateTime.Now.AddMinutes(5)) errors.Add("The transfer date cannot be in the future.");

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

                var wanted = request.Lines.GroupBy(l => l.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                await StockLedgerHelper2.EnsureDefaultLocationAsync(db);

                var from = await db.StockLocations2.FirstOrDefaultAsync(l => l.Id == request.FromLocationId && l.IsActive);
                var to = await db.StockLocations2.FirstOrDefaultAsync(l => l.Id == request.ToLocationId && l.IsActive);
                if (from == null) return StockResult2.Fail<int>("Source location not found or inactive.");
                if (to == null) return StockResult2.Fail<int>("Destination location not found or inactive.");

                var productIds = wanted.Keys.ToList();
                var names = await StockLedgerHelper2.LoadActiveProductNamesAsync(db, productIds);
                foreach (var pid in productIds.Where(pid => !names.ContainsKey(pid)))
                    errors.Add($"Product {pid} was not found or is inactive.");
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                await using var tx = await db.Database.BeginTransactionAsync();

                foreach (var pair in wanted)
                {
                    int available = await StockLedgerHelper2.GetQuantityAtLocationAsync(db, pair.Key, from);
                    if (pair.Value > available)
                        errors.Add($"'{names[pair.Key]}': only {Math.Max(available, 0)} available at {from.Name}, but {pair.Value} requested.");
                }
                if (errors.Count > 0) return StockResult2.Fail<int>(errors);

                string prefix = $"TRF-{date:yyyyMMdd}-";
                int existing = await db.StockTransfers2.CountAsync(t => t.TransferNumber.StartsWith(prefix));

                var transfer = new StockTransfer2
                {
                    TransferNumber = StockLedgerHelper2.BuildDocumentNumber("TRF", date, existing),
                    TransferDate = date,
                    FromLocationId = from.Id,
                    ToLocationId = to.Id,
                    Status = StockConstants2.StatusInTransit,
                    Remarks = Clean(request.Remarks, 500),
                    CreatedBy = userId,
                    CreatedOn = DateTime.Now
                };

                foreach (var pair in wanted)
                {
                    transfer.Lines.Add(new StockTransferLine2 { ProductId = pair.Key, Quantity = pair.Value });
                    // A default source is reduced automatically (it is derived: on hand - balances - in transit).
                    await StockLedgerHelper2.ChangeBalanceAsync(db, pair.Key, from, -pair.Value);
                }

                db.StockTransfers2.Add(transfer);
                await db.SaveChangesAsync();

                foreach (var pair in wanted)
                {
                    db.StockMovements2.Add(StockLedgerHelper2.NewMovement(
                        pair.Key, from.Id, StockConstants2.MoveTransferOut, -pair.Value, null,
                        StockConstants2.DocTransfer, transfer.Id, transfer.TransferNumber, null,
                        $"To {to.Name}", transfer.Remarks, userId));
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return StockResult2.Ok(transfer.Id, $"Transfer {transfer.TransferNumber} dispatched.");
            }, _logRepo, nameof(CreateTransferAsync), userId, request);
        }

        public Task<BaseResponse<bool>> ReceiveTransferAsync(int transferId, int? userId)
        {
            int uid = userId ?? StockConstants2.SystemUserId;

            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                await using var tx = await db.Database.BeginTransactionAsync();

                var transfer = await db.StockTransfers2
                    .Include(t => t.Lines)
                    .Include(t => t.ToLocation)
                    .FirstOrDefaultAsync(t => t.Id == transferId);

                if (transfer == null) return StockResult2.Fail<bool>("Transfer not found.");
                if (transfer.Status != StockConstants2.StatusInTransit)
                    return StockResult2.Fail<bool>($"Only transfers that are in transit can be received (current status: {transfer.Status}).");

                var to = transfer.ToLocation!;
                foreach (var line in transfer.Lines)
                {
                    // A default destination receives automatically once the line is no longer in transit.
                    await StockLedgerHelper2.ChangeBalanceAsync(db, line.ProductId, to, line.Quantity);
                }

                transfer.Status = StockConstants2.StatusReceived;
                transfer.ReceivedOn = DateTime.Now;
                transfer.ReceivedBy = uid;

                foreach (var line in transfer.Lines)
                {
                    db.StockMovements2.Add(StockLedgerHelper2.NewMovement(
                        line.ProductId, to.Id, StockConstants2.MoveTransferIn, line.Quantity, null,
                        StockConstants2.DocTransfer, transfer.Id, transfer.TransferNumber, null,
                        null, transfer.Remarks, uid));
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return StockResult2.Ok(true, $"Transfer {transfer.TransferNumber} received.");
            }, _logRepo, nameof(ReceiveTransferAsync), uid, new { transferId });
        }

        public Task<BaseResponse<bool>> CancelTransferAsync(int transferId, int? userId)
        {
            int uid = userId ?? StockConstants2.SystemUserId;

            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                await using var tx = await db.Database.BeginTransactionAsync();

                var transfer = await db.StockTransfers2
                    .Include(t => t.Lines)
                    .Include(t => t.FromLocation)
                    .FirstOrDefaultAsync(t => t.Id == transferId);

                if (transfer == null) return StockResult2.Fail<bool>("Transfer not found.");
                if (transfer.Status != StockConstants2.StatusInTransit)
                    return StockResult2.Fail<bool>($"Only transfers that are in transit can be cancelled (current status: {transfer.Status}).");

                var from = transfer.FromLocation!;
                foreach (var line in transfer.Lines)
                {
                    // The quantity goes back to the source location.
                    await StockLedgerHelper2.ChangeBalanceAsync(db, line.ProductId, from, line.Quantity);
                }

                transfer.Status = StockConstants2.StatusCancelled;
                transfer.CancelledOn = DateTime.Now;
                transfer.CancelledBy = uid;

                foreach (var line in transfer.Lines)
                {
                    db.StockMovements2.Add(StockLedgerHelper2.NewMovement(
                        line.ProductId, from.Id, StockConstants2.MoveTransferCancel, line.Quantity, null,
                        StockConstants2.DocTransfer, transfer.Id, transfer.TransferNumber, null,
                        "Cancelled", transfer.Remarks, uid));
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return StockResult2.Ok(true, $"Transfer {transfer.TransferNumber} cancelled.");
            }, _logRepo, nameof(CancelTransferAsync), uid, new { transferId });
        }

        public Task<BaseResponse<PagedResult2<DocumentRowDto2>>> GetTransfersAsync(StockQueryFilter2 filter)
        {
            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var query = db.StockTransfers2.AsNoTracking().AsQueryable();
                if (filter.LocationId.HasValue)
                    query = query.Where(t => t.FromLocationId == filter.LocationId.Value || t.ToLocationId == filter.LocationId.Value);
                if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(t => t.Status == filter.Status);
                if (filter.FromDate.HasValue) { var f = filter.FromDate.Value.Date; query = query.Where(t => t.TransferDate >= f); }
                if (filter.ToDate.HasValue) { var e = filter.ToDate.Value.Date.AddDays(1); query = query.Where(t => t.TransferDate < e); }
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    string s = filter.Search.Trim();
                    query = query.Where(t => t.TransferNumber.Contains(s)
                        || t.FromLocation!.Name.Contains(s) || t.ToLocation!.Name.Contains(s));
                }

                var rows = await query.Select(t => new DocumentRowDto2
                {
                    Id = t.Id,
                    Number = t.TransferNumber,
                    Date = t.TransferDate,
                    Status = t.Status,
                    Party = t.FromLocation!.Name + " -> " + t.ToLocation!.Name,
                    LineCount = t.Lines.Count,
                    TotalQuantity = t.Lines.Sum(l => l.Quantity),
                    Remarks = t.Remarks ?? string.Empty
                }).ToListAsync();

                return StockResult2.Ok(StockPaging2.ToPage(rows, filter, nameof(DocumentRowDto2.Date), true));
            }, _logRepo, nameof(GetTransfersAsync), null, filter);
        }

        public Task<BaseResponse<List<DocumentLineDto2>>> GetTransferLinesAsync(int transferId)
        {
            return StockResult2.RunAsync(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

                var lines = await db.StockTransferLines2.AsNoTracking()
                    .Where(l => l.TransferId == transferId)
                    .OrderBy(l => l.Id)
                    .Select(l => new DocumentLineDto2
                    {
                        LineId = l.Id,
                        ProductId = l.ProductId,
                        ProductCode = l.Product!.ProductCode,
                        ProductName = l.Product!.Name ?? string.Empty,
                        Quantity = l.Quantity
                    }).ToListAsync();

                return StockResult2.Ok(lines);
            }, _logRepo, nameof(GetTransferLinesAsync), null, new { transferId });
        }
    }
}
