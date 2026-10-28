using Common;
using Common.RetailPos2;
using Common.Stock2;
using Domain.Entities;
using Domain.IRepositories;
using Domain.RetailPos2;
using Domain.Stock2;
using Domain.Users2;
using Service.IService;

namespace Service.Service
{
    /// <summary>
    /// Sale returns. A return never edits the sale. It adds records: the return, a ledger credit (SaleReturn), a refund
    /// entry when money goes back to the client, and stock movements. Balances stay calculated from the rows.
    /// </summary>
    public class RetailPosReturnService2 : IRetailPosReturnService2
    {
        private readonly IRetailPosUnitOfWorkFactory2 _uowFactory;
        private readonly ILogRepository _logRepo;
        private readonly ICurrentUser2 _currentUser;

        public RetailPosReturnService2(IRetailPosUnitOfWorkFactory2 uowFactory, ILogRepository logRepo, ICurrentUser2 currentUser)
        {
            _uowFactory = uowFactory;
            _logRepo = logRepo;
            _currentUser = currentUser;
        }

        private static bool IsValidStockAction(string? action)
        {
            if (string.IsNullOrWhiteSpace(action)) return false;
            if (action == RetailPosConstants2.StockReturnToStock) return true;
            return StockConstants2.IssueReasons.Contains(action) && action != "Returned to supplier";
        }

        // ================================================================== prepare

        public Task<BaseResponse<RetailPosReturnPrepDto2>> GetReturnPrepAsync(int saleId)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.Check<RetailPosReturnPrepDto2>(_currentUser, ScreenCatalog2.RetailPosReports);
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                var row = await uow.Sales.GetSaleRowAsync(saleId);
                if (row == null) return RetailPosResult2.Fail<RetailPosReturnPrepDto2>("Invoice not found.");

                var lines = await uow.Returns.GetSaleLineEntitiesAsync(saleId);
                var returned = await uow.Returns.GetReturnedQuantitiesAsync(saleId);
                var stockLines = await uow.Returns.GetSaleStockLinesAsync(saleId);

                var prep = new RetailPosReturnPrepDto2
                {
                    Sale = RetailPosRules2.FinishSaleRow(row, DateTime.Today),
                    StockTracked = stockLines.Count > 0
                };

                foreach (var l in lines)
                {
                    returned.TryGetValue(l.Id, out int already);
                    prep.Lines.Add(new RetailPosReturnableLineDto2
                    {
                        SaleLineId = l.Id,
                        ProductId = l.ProductId,
                        ProductCode = l.ProductCode,
                        ProductName = l.ProductName,
                        SoldQuantity = l.Quantity,
                        ReturnedQuantity = already,
                        UnitPrice = l.UnitPrice,
                        CreditUnitPrice = RetailPosRules2.ReturnCredit(l.UnitPrice, 1, row.SubTotal, row.TotalAmount)
                    });
                }

                return RetailPosResult2.Ok(prep);
            }, _logRepo, nameof(GetReturnPrepAsync), _currentUser.UserId, new { saleId });
        }

        // ================================================================== return

        public Task<BaseResponse<RetailPosReturnResultDto2>> ReturnItemsAsync(RetailPosReturnRequestDto2 request)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.Check<RetailPosReturnResultDto2>(_currentUser, ScreenCatalog2.RetailPosReports);
                if (denied != null) return denied;

                int userId = _currentUser.UserId!.Value;
                DateTime now = DateTime.Now;
                DateTime returnDate = request.ReturnDate ?? now;
                string reason = (request.Reason ?? string.Empty).Trim();
                var errors = new List<string>();

                if (request.IdempotencyKey == Guid.Empty) errors.Add("The return form has no submission key. Reload the page and try again.");
                if (request.SaleId <= 0) errors.Add("Choose an invoice.");
                if (reason.Length < 3) errors.Add("Enter the reason for the return.");
                if (reason.Length > 250) errors.Add("The reason can be at most 250 characters.");
                if (returnDate > now.AddMinutes(5)) errors.Add("The return date cannot be in the future.");
                if (!RetailPosConstants2.PaymentMethods.Contains(request.RefundMethod)) errors.Add("Choose a valid refund method.");

                var wanted = (request.Lines ?? new()).Where(l => l.Quantity > 0).ToList();
                if (wanted.Count == 0) errors.Add("Enter the quantity to return for at least one product.");
                if (wanted.GroupBy(l => l.SaleLineId).Any(g => g.Count() > 1)) errors.Add("A product line is repeated.");
                foreach (var l in wanted)
                    if (!IsValidStockAction(l.StockAction)) errors.Add("Choose what happens to the returned stock for every product.");
                if (errors.Count > 0) return RetailPosResult2.Fail<RetailPosReturnResultDto2>(errors.Distinct().ToList());

                await using var uow = _uowFactory.Create();
                await uow.BeginTransactionAsync();

                // ---- the same form submitted twice returns the first result instead of returning the goods again
                var duplicate = await uow.Returns.FindReturnByKeyAsync(request.IdempotencyKey);
                if (duplicate != null)
                {
                    var existingBalance = await uow.Sales.GetSaleBalanceAsync(duplicate.SaleId);
                    return RetailPosResult2.Ok(new RetailPosReturnResultDto2
                    {
                        ReturnId = duplicate.Id,
                        ReturnNumber = duplicate.ReturnNumber,
                        CreditAmount = duplicate.CreditAmount,
                        RefundAmount = duplicate.RefundAmount,
                        InvoiceOutstandingAfter = existingBalance?.OutstandingAmount ?? 0m,
                        WasDuplicate = true
                    }, "This return was already saved.");
                }

                var sale = await uow.Sales.GetSaleAsync(request.SaleId);
                if (sale == null) return RetailPosResult2.Fail<RetailPosReturnResultDto2>("Invoice not found.");
                if (returnDate.Date < sale.SaleDate.Date) return RetailPosResult2.Fail<RetailPosReturnResultDto2>("The return date cannot be before the sale date.");

                var saleLines = (await uow.Returns.GetSaleLineEntitiesAsync(sale.Id)).ToDictionary(l => l.Id);
                var alreadyReturned = await uow.Returns.GetReturnedQuantitiesAsync(sale.Id);
                var stockLines = await uow.Returns.GetSaleStockLinesAsync(sale.Id);
                decimal creditedBefore = await uow.Returns.GetCreditedAmountAsync(sale.Id);

                foreach (var l in wanted)
                {
                    if (!saleLines.TryGetValue(l.SaleLineId, out var sl))
                    {
                        errors.Add("A product line does not belong to this invoice.");
                        continue;
                    }
                    alreadyReturned.TryGetValue(sl.Id, out int done);
                    int left = sl.Quantity - done;
                    if (l.Quantity > left)
                        errors.Add($"'{sl.ProductName}': only {Math.Max(left, 0)} can still be returned (sold {sl.Quantity}, already returned {done}).");
                }
                if (errors.Count > 0) return RetailPosResult2.Fail<RetailPosReturnResultDto2>(errors);

                // ---- credit for each line: the price the client really paid (after the invoice discount)
                var credits = wanted.ToDictionary(l => l.SaleLineId,
                    l => RetailPosRules2.ReturnCredit(saleLines[l.SaleLineId].UnitPrice, l.Quantity, sale.SubTotal, sale.TotalAmount));

                // When this return brings back everything that is left, the credit is trued up so rounding never leaves a few paisa behind.
                bool everythingReturned = saleLines.Values.All(sl =>
                {
                    alreadyReturned.TryGetValue(sl.Id, out int done);
                    int now2 = wanted.FirstOrDefault(w => w.SaleLineId == sl.Id)?.Quantity ?? 0;
                    return done + now2 >= sl.Quantity;
                });
                if (everythingReturned)
                {
                    decimal residual = RetailPosRules2.Round(sale.TotalAmount - creditedBefore - credits.Values.Sum());
                    if (residual != 0m)
                    {
                        int lastKey = wanted.Last().SaleLineId;
                        credits[lastKey] = RetailPosRules2.Round(credits[lastKey] + residual);
                    }
                }

                decimal credit = RetailPosRules2.Round(credits.Values.Sum());

                // ---- money: the credit first reduces what the client still owes; only the rest is paid back
                var balance = await uow.Sales.GetSaleBalanceAsync(sale.Id);
                decimal outstandingBefore = balance?.OutstandingAmount ?? 0m;
                decimal refund = RetailPosRules2.RefundDue(outstandingBefore, credit);

                // ---- document number
                string prefix = RetailPosRules2.DocumentPrefix(RetailPosConstants2.ReturnPrefix, returnDate);
                int existing = await uow.Returns.CountReturnsWithPrefixAsync(prefix);

                var saleReturn = new RetailPosReturn2
                {
                    ReturnNumber = RetailPosRules2.BuildDocumentNumber(RetailPosConstants2.ReturnPrefix, returnDate, existing),
                    SaleId = sale.Id,
                    ClientId = sale.ClientId,
                    ReturnDate = returnDate,
                    CreditAmount = credit,
                    RefundAmount = refund,
                    RefundMethod = refund > 0m ? request.RefundMethod : null,
                    Reason = reason,
                    IdempotencyKey = request.IdempotencyKey,
                    CreatedBy = userId,
                    CreatedOn = now
                };

                // ---- stock (only for lines the original sale took out of stock)
                var stockEffects = new List<(RetailPosReturnLine2 Line, int Quantity, decimal UnitCost, Stock? Row, string Action)>();
                var locations = new Dictionary<int, StockLocation2>();

                foreach (var l in wanted)
                {
                    var sl = saleLines[l.SaleLineId];
                    var line = new RetailPosReturnLine2
                    {
                        SaleLineId = sl.Id,
                        ProductId = sl.ProductId,
                        ProductCode = sl.ProductCode,
                        ProductName = sl.ProductName,
                        Quantity = l.Quantity,
                        UnitPrice = sl.UnitPrice,
                        LineAmount = credits[sl.Id],
                        StockAction = l.StockAction
                    };
                    saleReturn.Lines.Add(line);

                    if (!stockLines.TryGetValue(sl.ProductId, out var original)) continue; // old invoice: no stock record, nothing to change

                    if (!locations.TryGetValue(original.LocationId, out var location))
                    {
                        var found = await uow.Stock.GetLocationAsync(original.LocationId, false);
                        if (found == null) return RetailPosResult2.Fail<RetailPosReturnResultDto2>("The stock location of this sale no longer exists.");
                        location = found;
                        locations[original.LocationId] = location;
                    }

                    line.LocationId = location.Id;
                    Stock? row = null;

                    if (l.StockAction == RetailPosConstants2.StockReturnToStock)
                    {
                        // The goods go back on the shelf at the cost they left at.
                        row = StockLedgerRules2.NewStockInRow(sl.ProductId, l.Quantity, RetailPosRules2.Round(l.Quantity * original.UnitCost), null, userId);
                        uow.Stock.AddLedgerRow(row);
                        await uow.Stock.ChangeBalanceAsync(sl.ProductId, location, l.Quantity);
                    }

                    stockEffects.Add((line, l.Quantity, original.UnitCost, row, l.StockAction));
                }

                // ---- ledger entries
                if (credit > 0m)
                {
                    uow.Payments.AddAdjustment(new RetailPosAdjustment2
                    {
                        ClientId = sale.ClientId,
                        SaleId = sale.Id,
                        AdjustmentType = RetailPosConstants2.AdjustmentSaleReturn,
                        Amount = -credit,
                        Reason = $"Return {saleReturn.ReturnNumber} of invoice {sale.InvoiceNumber}: {reason}",
                        CreatedBy = userId,
                        CreatedOn = now
                    });
                }
                if (refund > 0m)
                {
                    uow.Payments.AddAdjustment(new RetailPosAdjustment2
                    {
                        ClientId = sale.ClientId,
                        SaleId = sale.Id,
                        AdjustmentType = RetailPosConstants2.AdjustmentRefund,
                        Amount = refund,
                        Reason = $"Refund of {refund:N2} paid back ({request.RefundMethod}) for return {saleReturn.ReturnNumber}",
                        CreatedBy = userId,
                        CreatedOn = now
                    });
                }

                // Touch the invoice and client so a parallel payment or return cannot slip past the balance check.
                sale.ModifiedOn = now;
                if (sale.ClientId.HasValue)
                {
                    var client = await uow.Clients.GetClientAsync(sale.ClientId.Value);
                    if (client != null) client.ModifiedOn = now;
                }

                uow.Returns.AddReturn(saleReturn);
                await uow.SaveChangesAsync(); // the return now has its Id

                foreach (var s in stockEffects)
                {
                    string note = $"Retail POS return of invoice {sale.InvoiceNumber}";
                    // The goods always come back in...
                    uow.Stock.AddMovement(StockLedgerRules2.NewMovement(
                        s.Line.ProductId, s.Line.LocationId, StockConstants2.MoveReturn, s.Quantity, s.UnitCost,
                        RetailPosConstants2.StockReturnDocumentType, saleReturn.Id, saleReturn.ReturnNumber, s.Row,
                        null, note, userId));

                    // ...and a write-off sends them straight out again, so the movement report shows the real reason.
                    if (s.Action != RetailPosConstants2.StockReturnToStock)
                    {
                        uow.Stock.AddMovement(StockLedgerRules2.NewMovement(
                            s.Line.ProductId, s.Line.LocationId, StockConstants2.MoveIssue, -s.Quantity, s.UnitCost,
                            RetailPosConstants2.StockReturnDocumentType, saleReturn.Id, saleReturn.ReturnNumber, null,
                            s.Action, note, userId));
                    }
                }

                await uow.SaveChangesAsync();
                await uow.CommitAsync();

                string stockNote = stockEffects.Count == 0 ? " Stock was not changed (this invoice has no stock record)." : string.Empty;
                return RetailPosResult2.Ok(new RetailPosReturnResultDto2
                {
                    ReturnId = saleReturn.Id,
                    ReturnNumber = saleReturn.ReturnNumber,
                    CreditAmount = credit,
                    RefundAmount = refund,
                    InvoiceOutstandingAfter = Math.Max(0m, RetailPosRules2.Round(outstandingBefore - credit))
                }, $"Return {saleReturn.ReturnNumber} saved. Credit {credit:N2}" + (refund > 0m ? $", refund to pay back {refund:N2}." : ".") + stockNote);
            }, _logRepo, nameof(ReturnItemsAsync), _currentUser.UserId, request);
        }

        public Task<BaseResponse<List<RetailPosReturnRowDto2>>> GetReturnsAsync(RetailPosReturnFilterDto2 filter)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.Check<List<RetailPosReturnRowDto2>>(_currentUser, ScreenCatalog2.RetailPosReports);
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                return RetailPosResult2.Ok(await uow.Returns.GetReturnRowsAsync(filter ?? new RetailPosReturnFilterDto2()));
            }, _logRepo, nameof(GetReturnsAsync), _currentUser.UserId, filter);
        }
    }
}
