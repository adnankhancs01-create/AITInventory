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
    /// <summary>Retail POS checkout and invoice queries. Money rules live in RetailPosRules2.</summary>
    public class RetailPosSaleService2 : IRetailPosSaleService2
    {
        private readonly IRetailPosUnitOfWorkFactory2 _uowFactory;
        private readonly ILogRepository _logRepo;
        private readonly ICurrentUser2 _currentUser;

        public RetailPosSaleService2(IRetailPosUnitOfWorkFactory2 uowFactory, ILogRepository logRepo, ICurrentUser2 currentUser)
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

        public Task<BaseResponse<List<RetailPosProductLookupDto2>>> GetProductLookupAsync()
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.RequireLogin<List<RetailPosProductLookupDto2>>(_currentUser);
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                return RetailPosResult2.Ok(await uow.Sales.GetProductLookupAsync());
            }, _logRepo, nameof(GetProductLookupAsync), _currentUser.UserId);
        }

        public Task<BaseResponse<List<StockLocationDto2>>> GetLocationsAsync()
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.RequireLogin<List<StockLocationDto2>>(_currentUser);
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                await uow.Stock.EnsureDefaultLocationAsync();
                return RetailPosResult2.Ok(await uow.StockMaster.GetLocationDtosAsync(false));
            }, _logRepo, nameof(GetLocationsAsync), _currentUser.UserId);
        }

        /// <summary>Quantity available to sell per product at one location (null = default location). Same rules the checkout uses.</summary>
        public Task<BaseResponse<Dictionary<int, int>>> GetAvailableStockAsync(int? locationId)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.RequireLogin<Dictionary<int, int>>(_currentUser);
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                StockLocation2? location = locationId is > 0
                    ? await uow.Stock.GetLocationAsync(locationId.Value, true)
                    : await uow.Stock.EnsureDefaultLocationAsync();
                if (location == null) return RetailPosResult2.Fail<Dictionary<int, int>>("Stock location not found or inactive.");

                var states = await uow.Stock.GetStatesAsync();
                var balanceTotals = await uow.Stock.GetBalanceTotalsAsync();
                var inTransit = await uow.Stock.GetInTransitAsync();
                var atLocation = location.IsDefault ? new Dictionary<int, int>() : await uow.Stock.GetLocationBalancesAsync(location.Id);

                var result = new Dictionary<int, int>();
                var productIds = states.Keys.Concat(atLocation.Keys).Distinct();
                foreach (int productId in productIds)
                {
                    int onHand = states.TryGetValue(productId, out var state) ? state.OnHand : 0;
                    result[productId] = StockLedgerRules2.QuantityAtLocation(
                        onHand,
                        StockLedgerRules2.Get(balanceTotals, productId),
                        StockLedgerRules2.Get(inTransit, productId),
                        location.IsDefault,
                        StockLedgerRules2.Get(atLocation, productId));
                }

                return RetailPosResult2.Ok(result);
            }, _logRepo, nameof(GetAvailableStockAsync), _currentUser.UserId, new { locationId });
        }

        // ================================================================== checkout

        public Task<BaseResponse<RetailPosCheckoutResultDto2>> CheckoutAsync(RetailPosCheckoutRequestDto2 request)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.Check<RetailPosCheckoutResultDto2>(_currentUser, ScreenCatalog2.RetailPosCheckout);
                if (denied != null) return denied;

                int userId = _currentUser.UserId!.Value;
                DateTime now = DateTime.Now;
                DateTime saleDate = request.SaleDate ?? now;
                var errors = new List<string>();

                if (request.IdempotencyKey == Guid.Empty)
                    errors.Add("The checkout form has no submission key. Reload the page and try again.");

                if (request.Lines == null || request.Lines.Count == 0)
                {
                    errors.Add("Add at least one product.");
                }
                else
                {
                    for (int i = 0; i < request.Lines.Count; i++)
                    {
                        var line = request.Lines[i];
                        string n = $"Line {i + 1}: ";
                        if (line.ProductId <= 0) errors.Add(n + "choose a product.");
                        if (line.Quantity <= 0) errors.Add(n + "quantity must be greater than zero.");
                        if (line.Quantity > 1_000_000) errors.Add(n + "quantity is too large.");
                        if (line.UnitPrice < 0m) errors.Add(n + "price cannot be negative.");
                        if (line.UnitPrice > RetailPosConstants2.MaxAmount) errors.Add(n + "price is too large.");
                        if (line.DiscountPercent < 0m || line.DiscountPercent > 100m) errors.Add(n + "discount must be between 0% and 100%.");
                    }
                }

                decimal discount = RetailPosRules2.Round(request.DiscountAmount);
                if (discount < 0m) errors.Add("Discount cannot be negative.");
                if (saleDate > now.AddMinutes(5)) errors.Add("The sale date cannot be in the future.");
                if (request.DueDate.HasValue && request.DueDate.Value.Date < saleDate.Date)
                    errors.Add("The due date cannot be before the sale date.");
                if ((request.PaymentReference?.Trim().Length ?? 0) > 100) errors.Add("Payment reference can be at most 100 characters.");
                if ((request.Remarks?.Trim().Length ?? 0) > 500) errors.Add("Remarks can be at most 500 characters.");
                if (errors.Count > 0) return RetailPosResult2.Fail<RetailPosCheckoutResultDto2>(errors);

                decimal subTotal = RetailPosRules2.Round(request.Lines.Sum(l => RetailPosRules2.Round(l.Quantity * l.UnitPrice)));

                // The invoice discount is the discount typed for the whole invoice plus the percent discounts of the lines.
                decimal lineDiscounts = request.Lines.Sum(l => RetailPosRules2.LineDiscount(RetailPosRules2.Round(l.Quantity * l.UnitPrice), l.DiscountPercent));
                discount = RetailPosRules2.Round(discount + lineDiscounts);

                if (discount > subTotal)
                    return RetailPosResult2.Fail<RetailPosCheckoutResultDto2>("The discount cannot be more than the subtotal.");
                decimal total = RetailPosRules2.Round(subTotal - discount);

                string? paymentError = RetailPosRules2.CheckoutPayment(
                    request.PaymentType, total, request.PaidNow, request.ClientId.HasValue, out decimal paidNow);
                if (paymentError != null) return RetailPosResult2.Fail<RetailPosCheckoutResultDto2>(paymentError);
                if (paidNow > 0m && !RetailPosConstants2.PaymentMethods.Contains(request.PaymentMethod))
                    return RetailPosResult2.Fail<RetailPosCheckoutResultDto2>("Choose a valid payment method.");

                decimal outstanding = RetailPosRules2.Round(total - paidNow);

                await using var uow = _uowFactory.Create();
                await uow.BeginTransactionAsync();

                // ---- the same form submitted twice returns the first invoice instead of creating another
                var duplicate = await uow.Sales.FindSaleByKeyAsync(request.IdempotencyKey);
                if (duplicate != null)
                {
                    var existingBalance = await uow.Sales.GetSaleBalanceAsync(duplicate.Id);
                    return RetailPosResult2.Ok(new RetailPosCheckoutResultDto2
                    {
                        SaleId = duplicate.Id,
                        InvoiceNumber = duplicate.InvoiceNumber,
                        TotalAmount = duplicate.TotalAmount,
                        PaidAmount = existingBalance?.PaidAmount ?? 0m,
                        OutstandingAmount = existingBalance?.OutstandingAmount ?? 0m,
                        PaymentType = duplicate.PaymentType,
                        WasDuplicate = true
                    }, "This sale was already saved.");
                }

                // ---- products (name and code are copied into the invoice)
                var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
                var products = await uow.Sales.GetActiveProductsAsync(productIds);
                foreach (var id in productIds.Where(id => !products.ContainsKey(id)))
                    errors.Add($"Product {id} was not found or is inactive.");

                // ---- client
                RetailPosClient2? client = null;
                if (request.ClientId.HasValue)
                {
                    client = await uow.Clients.GetClientAsync(request.ClientId.Value);
                    if (client == null) errors.Add("Client not found.");
                    else if (!client.IsActive) errors.Add("This client is inactive.");
                }
                if (errors.Count > 0) return RetailPosResult2.Fail<RetailPosCheckoutResultDto2>(errors);

                // ---- stock: the goods leave the chosen location in the same transaction as the sale
                StockLocation2? location = request.LocationId is > 0
                    ? await uow.Stock.GetLocationAsync(request.LocationId.Value, true)
                    : await uow.Stock.EnsureDefaultLocationAsync();
                if (location == null) return RetailPosResult2.Fail<RetailPosCheckoutResultDto2>("Stock location not found or inactive.");

                var wanted = request.Lines.GroupBy(l => l.ProductId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
                foreach (var pair in wanted)
                {
                    int available = await uow.Stock.GetQuantityAtLocationAsync(pair.Key, location);
                    if (pair.Value > available)
                        errors.Add($"'{products[pair.Key].Name}': only {Math.Max(available, 0)} in stock at {location.Name}, but {pair.Value} requested.");
                }
                if (errors.Count > 0) return RetailPosResult2.Fail<RetailPosCheckoutResultDto2>(errors);

                // ---- credit limit (read inside the transaction)
                bool overridden = false;
                if (client != null && outstanding > 0m)
                {
                    decimal currentBalance = await uow.Clients.GetBalanceAsync(client.Id);
                    if (RetailPosRules2.ExceedsCreditLimit(client.CreditLimit, currentBalance, outstanding))
                    {
                        if (!_currentUser.IsRoot)
                        {
                            return RetailPosResult2.Fail<RetailPosCheckoutResultDto2>(
                                $"This sale would take {client.Name} to {currentBalance + outstanding:N2}, above the credit limit of {client.CreditLimit:N2}. The business owner must approve it.");
                        }
                        overridden = true;
                    }

                    // Changing the client row makes a parallel credit sale for the same client fail the concurrency check.
                    client.ModifiedOn = now;
                }

                // ---- invoice
                string invoicePrefix = RetailPosRules2.DocumentPrefix(RetailPosConstants2.InvoicePrefix, saleDate);
                int invoicesToday = await uow.Sales.CountSalesWithPrefixAsync(invoicePrefix);

                var sale = new RetailPosSale2
                {
                    InvoiceNumber = RetailPosRules2.BuildDocumentNumber(RetailPosConstants2.InvoicePrefix, saleDate, invoicesToday),
                    SaleDate = saleDate,
                    ClientId = client?.Id,
                    SubTotal = subTotal,
                    DiscountAmount = discount,
                    TotalAmount = total,
                    PaymentType = request.PaymentType,
                    DueDate = outstanding > 0m ? request.DueDate?.Date : null,
                    CreditLimitOverridden = overridden,
                    Remarks = Clean(request.Remarks, 500),
                    IdempotencyKey = request.IdempotencyKey,
                    CreatedBy = userId,
                    CreatedOn = now
                };

                foreach (var line in request.Lines)
                {
                    var product = products[line.ProductId];
                    sale.Lines.Add(new RetailPosSaleLine2
                    {
                        ProductId = product.Id,
                        ProductCode = product.Code,
                        ProductName = product.Name,
                        Quantity = line.Quantity,
                        UnitPrice = RetailPosRules2.Round(line.UnitPrice),
                        LineTotal = RetailPosRules2.Round(line.Quantity * line.UnitPrice)
                    });
                }

                // ---- first payment, saved with the invoice
                if (paidNow > 0m)
                {
                    string receiptPrefix = RetailPosRules2.DocumentPrefix(RetailPosConstants2.ReceiptPrefix, saleDate);
                    int receiptsToday = await uow.Payments.CountPaymentsWithPrefixAsync(receiptPrefix);

                    sale.Payments.Add(new RetailPosPayment2
                    {
                        PaymentNumber = RetailPosRules2.BuildDocumentNumber(RetailPosConstants2.ReceiptPrefix, saleDate, receiptsToday),
                        IdempotencyKey = request.IdempotencyKey,
                        ClientId = client?.Id,
                        Amount = paidNow,
                        PaymentDate = saleDate,
                        Method = request.PaymentMethod,
                        Reference = Clean(request.PaymentReference, 100),
                        Remarks = "Received at checkout",
                        CreatedBy = userId,
                        CreatedOn = now
                    });
                }

                // ---- stock-out rows (same shape the stock module uses) and location balances
                var states = await uow.Stock.GetStatesAsync(wanted.Keys.ToList());
                var stockOut = new List<(int ProductId, int Quantity, decimal UnitCost, Stock Row)>();
                foreach (var pair in wanted)
                {
                    states.TryGetValue(pair.Key, out var state);
                    decimal unitCost = StockLayerCalculator2.EstimateIssueCost(state, pair.Value);

                    var row = StockLedgerRules2.NewStockOutRow(pair.Key, pair.Value, userId);
                    uow.Stock.AddLedgerRow(row);
                    await uow.Stock.ChangeBalanceAsync(pair.Key, location, -pair.Value);
                    stockOut.Add((pair.Key, pair.Value, unitCost, row));
                }

                uow.Sales.AddSale(sale);
                await uow.SaveChangesAsync(); // the sale now has its Id

                foreach (var s in stockOut)
                {
                    uow.Stock.AddMovement(StockLedgerRules2.NewMovement(
                        s.ProductId, location.Id, StockConstants2.MoveSale, -s.Quantity, s.UnitCost,
                        RetailPosConstants2.StockDocumentType, sale.Id, sale.InvoiceNumber, s.Row,
                        null, "Retail POS sale", userId));
                }

                await uow.SaveChangesAsync();
                await uow.CommitAsync();

                return RetailPosResult2.Ok(new RetailPosCheckoutResultDto2
                {
                    SaleId = sale.Id,
                    InvoiceNumber = sale.InvoiceNumber,
                    TotalAmount = total,
                    PaidAmount = paidNow,
                    OutstandingAmount = outstanding,
                    PaymentType = sale.PaymentType
                }, $"Invoice {sale.InvoiceNumber} saved.");
            }, _logRepo, nameof(CheckoutAsync), _currentUser.UserId, request);
        }

        // ================================================================== queries

        public Task<BaseResponse<List<RetailPosSaleRowDto2>>> GetSalesAsync(RetailPosSaleFilterDto2 filter)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.RequireLogin<List<RetailPosSaleRowDto2>>(_currentUser);
                if (denied != null) return denied;

                filter ??= new RetailPosSaleFilterDto2();
                DateTime today = DateTime.Today;

                await using var uow = _uowFactory.Create();
                var rows = (await uow.Sales.GetSaleRowsAsync(filter))
                    .Select(r => RetailPosRules2.FinishSaleRow(r, today))
                    .Where(r => !filter.OnlyOutstanding || r.OutstandingAmount > 0m)
                    .Where(r => !filter.OnlyOverdue || r.IsOverdue)
                    .Where(r => string.IsNullOrWhiteSpace(filter.PaymentStatus) || r.PaymentStatus == filter.PaymentStatus)
                    .ToList();

                return RetailPosResult2.Ok(rows);
            }, _logRepo, nameof(GetSalesAsync), _currentUser.UserId, filter);
        }

        public Task<BaseResponse<RetailPosSaleDetailDto2>> GetSaleDetailAsync(int saleId)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.RequireLogin<RetailPosSaleDetailDto2>(_currentUser);
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                var row = await uow.Sales.GetSaleRowAsync(saleId);
                if (row == null) return RetailPosResult2.Fail<RetailPosSaleDetailDto2>("Invoice not found.");

                var detail = new RetailPosSaleDetailDto2
                {
                    Sale = RetailPosRules2.FinishSaleRow(row, DateTime.Today),
                    Lines = await uow.Sales.GetSaleLinesAsync(saleId),
                    Payments = await uow.Payments.GetPaymentRowsAsync(new RetailPosPaymentFilterDto2 { SaleId = saleId, IncludeReversed = true })
                };

                return RetailPosResult2.Ok(detail);
            }, _logRepo, nameof(GetSaleDetailAsync), _currentUser.UserId, new { saleId });
        }
    }
}
