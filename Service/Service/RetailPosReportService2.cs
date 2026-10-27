using Common;
using Common.RetailPos2;
using Domain.IRepositories;
using Domain.RetailPos2;
using Domain.Users2;
using Service.IService;

namespace Service.Service
{
    /// <summary>Client statements and the receivables report. Everything is calculated from the recorded sales, payments and adjustments.</summary>
    public class RetailPosReportService2 : IRetailPosReportService2
    {
        private readonly IRetailPosUnitOfWorkFactory2 _uowFactory;
        private readonly ILogRepository _logRepo;
        private readonly ICurrentUser2 _currentUser;

        public RetailPosReportService2(IRetailPosUnitOfWorkFactory2 uowFactory, ILogRepository logRepo, ICurrentUser2 currentUser)
        {
            _uowFactory = uowFactory;
            _logRepo = logRepo;
            _currentUser = currentUser;
        }

        private sealed class Entry
        {
            public DateTime Date { get; init; }
            public int Order { get; init; }
            public string Type { get; init; } = string.Empty;
            public string Reference { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public decimal Debit { get; init; }
            public decimal Credit { get; init; }
            public string User { get; init; } = string.Empty;
        }

        public Task<BaseResponse<RetailPosStatementDto2>> GetStatementAsync(int clientId, DateTime? fromDate, DateTime? toDate)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.Check<RetailPosStatementDto2>(_currentUser, ScreenCatalog2.RetailPosStatement);
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();

                var client = await uow.Clients.FindClientAsync(clientId);
                if (client == null) return RetailPosResult2.Fail<RetailPosStatementDto2>("Client not found.");

                var sales = await uow.Sales.GetSaleRowsAsync(new RetailPosSaleFilterDto2 { ClientId = clientId });
                var payments = await uow.Payments.GetPaymentRowsAsync(new RetailPosPaymentFilterDto2 { ClientId = clientId, IncludeReversed = true });
                var adjustments = await uow.Payments.GetAdjustmentsAsync(clientId);
                var userNames = await uow.Payments.GetUserNamesAsync(adjustments.Select(a => a.CreatedBy).Distinct().ToList());
                var invoiceNumbers = sales.ToDictionary(s => s.Id, s => s.InvoiceNumber);

                var entries = new List<Entry>();

                if (client.OpeningBalance != 0m)
                {
                    entries.Add(new Entry
                    {
                        Date = client.CreatedOn, Order = 0, Type = RetailPosConstants2.EntryOpening,
                        Reference = "OPENING", Description = "Balance before this system",
                        Debit = client.OpeningBalance
                    });
                }

                foreach (var s in sales)
                {
                    entries.Add(new Entry
                    {
                        Date = s.SaleDate, Order = 1, Type = RetailPosConstants2.EntrySale,
                        Reference = s.InvoiceNumber, Description = $"Sale ({s.PaymentType})",
                        Debit = s.TotalAmount
                    });
                }

                foreach (var p in payments)
                {
                    string target = string.IsNullOrEmpty(p.InvoiceNumber) ? "account balance" : $"invoice {p.InvoiceNumber}";
                    entries.Add(new Entry
                    {
                        Date = p.PaymentDate, Order = 2, Type = RetailPosConstants2.EntryPayment,
                        Reference = p.PaymentNumber, Description = $"{p.Method} payment against {target}",
                        Credit = p.Amount, User = p.ReceivedBy
                    });

                    if (p.IsReversed && p.ReversedOn.HasValue)
                    {
                        entries.Add(new Entry
                        {
                            Date = p.ReversedOn.Value, Order = 3, Type = RetailPosConstants2.EntryPaymentReversal,
                            Reference = p.PaymentNumber, Description = $"Reversed: {p.ReversalReason}",
                            Debit = p.Amount
                        });
                    }
                }

                foreach (var a in adjustments)
                {
                    string invoice = a.SaleId.HasValue && invoiceNumbers.TryGetValue(a.SaleId.Value, out var number) ? $" (invoice {number})" : string.Empty;
                    userNames.TryGetValue(a.CreatedBy, out var who);

                    entries.Add(new Entry
                    {
                        Date = a.CreatedOn, Order = 4,
                        Type = a.AdjustmentType == RetailPosConstants2.AdjustmentCreditNote ? RetailPosConstants2.EntryCreditNote : RetailPosConstants2.EntryAdjustment,
                        Reference = $"ADJ-{a.Id}", Description = a.Reason + invoice,
                        Debit = a.Amount > 0m ? a.Amount : 0m,
                        Credit = a.Amount < 0m ? -a.Amount : 0m,
                        User = who ?? string.Empty
                    });
                }

                var ordered = entries.OrderBy(e => e.Date).ThenBy(e => e.Order).ThenBy(e => e.Reference, StringComparer.Ordinal).ToList();

                var statement = new RetailPosStatementDto2
                {
                    ClientId = client.Id,
                    ClientName = client.Name,
                    Phone = client.Phone ?? string.Empty,
                    FromDate = fromDate,
                    ToDate = toDate
                };

                decimal running = 0m;
                bool broughtForwardAdded = false;
                DateTime? from = fromDate?.Date;
                DateTime? toExclusive = toDate?.Date.AddDays(1);

                foreach (var e in ordered)
                {
                    if (toExclusive.HasValue && e.Date >= toExclusive.Value) break;

                    if (from.HasValue && e.Date < from.Value)
                    {
                        running += e.Debit - e.Credit;
                        continue;
                    }

                    if (from.HasValue && !broughtForwardAdded)
                    {
                        broughtForwardAdded = true;
                        statement.Rows.Add(new RetailPosStatementRowDto2
                        {
                            Date = from.Value,
                            EntryType = RetailPosConstants2.EntryBroughtForward,
                            Description = "Balance before the selected period",
                            Balance = running
                        });
                    }

                    running += e.Debit - e.Credit;
                    statement.TotalDebit += e.Debit;
                    statement.TotalCredit += e.Credit;
                    statement.Rows.Add(new RetailPosStatementRowDto2
                    {
                        Date = e.Date,
                        EntryType = e.Type,
                        Reference = e.Reference,
                        Description = e.Description,
                        Debit = e.Debit,
                        Credit = e.Credit,
                        Balance = running,
                        User = e.User
                    });
                }

                if (from.HasValue && !broughtForwardAdded)
                {
                    statement.Rows.Add(new RetailPosStatementRowDto2
                    {
                        Date = from.Value,
                        EntryType = RetailPosConstants2.EntryBroughtForward,
                        Description = "Balance before the selected period",
                        Balance = running
                    });
                }

                statement.ClosingBalance = running;
                return RetailPosResult2.Ok(statement);
            }, _logRepo, nameof(GetStatementAsync), _currentUser.UserId, new { clientId, fromDate, toDate });
        }

        public Task<BaseResponse<RetailPosReceivablesDto2>> GetReceivablesAsync(DateTime? asOfDate)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.Check<RetailPosReceivablesDto2>(_currentUser, ScreenCatalog2.RetailPosReports);
                if (denied != null) return denied;

                DateTime today = (asOfDate ?? DateTime.Today).Date;

                await using var uow = _uowFactory.Create();
                var clients = await uow.Clients.GetClientRowsAsync(new RetailPosClientFilterDto2 { OnlyWithBalance = true });
                var overdue = (await uow.Sales.GetSaleRowsAsync(new RetailPosSaleFilterDto2 { OnlyOutstanding = true }))
                    .Select(r => RetailPosRules2.FinishSaleRow(r, today))
                    .Where(r => r.ClientId.HasValue && r.IsOverdue)
                    .GroupBy(r => r.ClientId!.Value)
                    .ToDictionary(g => g.Key, g => new { Amount = g.Sum(x => x.OutstandingAmount), Oldest = g.Min(x => x.DueDate) });

                var result = new RetailPosReceivablesDto2();
                foreach (var c in clients.OrderByDescending(c => c.Balance))
                {
                    overdue.TryGetValue(c.Id, out var o);
                    result.Rows.Add(new RetailPosReceivableRowDto2
                    {
                        ClientId = c.Id,
                        ClientName = c.Name,
                        Phone = c.Phone,
                        OpeningBalance = c.OpeningBalance,
                        TotalSales = c.TotalSales,
                        TotalPaid = c.TotalPaid,
                        TotalAdjustments = c.TotalAdjustments,
                        Balance = c.Balance,
                        OverdueAmount = o?.Amount ?? 0m,
                        OldestDueDate = o?.Oldest
                    });
                }

                result.TotalReceivable = result.Rows.Sum(r => r.Balance);
                result.TotalOverdue = result.Rows.Sum(r => r.OverdueAmount);
                return RetailPosResult2.Ok(result);
            }, _logRepo, nameof(GetReceivablesAsync), _currentUser.UserId, new { asOfDate });
        }
    }
}
