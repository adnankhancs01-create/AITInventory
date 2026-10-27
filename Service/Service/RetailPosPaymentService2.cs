using Common;
using Common.RetailPos2;
using Domain.Entities;
using Domain.IRepositories;
using Domain.RetailPos2;
using Domain.Users2;
using Service.IService;

namespace Service.Service
{
    /// <summary>
    /// Receiving payments, reversals and balance adjustments. A payment row is never edited: a new payment is a new row,
    /// and a correction is a reversal or an adjustment by the owner. Balances are always calculated from the rows.
    /// </summary>
    public class RetailPosPaymentService2 : IRetailPosPaymentService2
    {
        private readonly IRetailPosUnitOfWorkFactory2 _uowFactory;
        private readonly ILogRepository _logRepo;
        private readonly ICurrentUser2 _currentUser;

        public RetailPosPaymentService2(IRetailPosUnitOfWorkFactory2 uowFactory, ILogRepository logRepo, ICurrentUser2 currentUser)
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

        // ================================================================== receive payment

        public Task<BaseResponse<RetailPosReceiptDto2>> ReceivePaymentAsync(RetailPosReceivePaymentRequestDto2 request)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.Check<RetailPosReceiptDto2>(_currentUser, ScreenCatalog2.RetailPosReceivePayment);
                if (denied != null) return denied;

                int userId = _currentUser.UserId!.Value;
                DateTime now = DateTime.Now;
                DateTime paymentDate = request.PaymentDate ?? now;
                decimal amount = RetailPosRules2.Round(request.Amount);
                var errors = new List<string>();

                if (request.IdempotencyKey == Guid.Empty)
                    errors.Add("The payment form has no submission key. Reload the page and try again.");
                if (request.ClientId <= 0) errors.Add("Select a client.");
                if (amount <= 0m) errors.Add("The payment amount must be greater than zero.");
                if (amount > RetailPosConstants2.MaxAmount) errors.Add("The payment amount is too large.");
                if (!RetailPosConstants2.PaymentMethods.Contains(request.Method)) errors.Add("Choose a valid payment method.");
                if (paymentDate > now.AddMinutes(5)) errors.Add("The payment date cannot be in the future.");
                if ((request.Reference?.Trim().Length ?? 0) > 100) errors.Add("Reference can be at most 100 characters.");
                if ((request.Remarks?.Trim().Length ?? 0) > 250) errors.Add("Remarks can be at most 250 characters.");
                if (errors.Count > 0) return RetailPosResult2.Fail<RetailPosReceiptDto2>(errors);

                await using var uow = _uowFactory.Create();
                await uow.BeginTransactionAsync();

                // ---- the same form submitted twice returns the first receipt instead of recording a second payment
                var duplicate = await uow.Payments.FindPaymentByKeyAsync(request.IdempotencyKey);
                if (duplicate != null)
                {
                    var receipt = await BuildReceiptAsync(uow, duplicate.Id);
                    if (receipt != null) receipt.WasDuplicate = true;
                    return receipt == null
                        ? RetailPosResult2.Fail<RetailPosReceiptDto2>("Payment not found.")
                        : RetailPosResult2.Ok(receipt, "This payment was already saved.");
                }

                var client = await uow.Clients.GetClientAsync(request.ClientId);
                if (client == null) return RetailPosResult2.Fail<RetailPosReceiptDto2>("Client not found.");

                if (request.SaleId.HasValue)
                {
                    var balance = await uow.Sales.GetSaleBalanceAsync(request.SaleId.Value);
                    if (balance == null) return RetailPosResult2.Fail<RetailPosReceiptDto2>("Invoice not found.");
                    if (balance.ClientId != client.Id)
                        return RetailPosResult2.Fail<RetailPosReceiptDto2>("This invoice does not belong to the selected client.");

                    decimal outstanding = balance.OutstandingAmount;
                    if (outstanding <= 0m)
                        return RetailPosResult2.Fail<RetailPosReceiptDto2>($"Invoice {balance.InvoiceNumber} is already fully paid.");
                    if (amount > outstanding)
                        return RetailPosResult2.Fail<RetailPosReceiptDto2>(
                            $"The payment of {amount:N2} is more than the {outstanding:N2} outstanding on invoice {balance.InvoiceNumber}.");

                    // Changing the invoice header makes two parallel payments on the same invoice fail the concurrency check.
                    var sale = await uow.Sales.GetSaleAsync(request.SaleId.Value);
                    if (sale != null) sale.ModifiedOn = now;
                }
                else
                {
                    decimal clientBalance = await uow.Clients.GetBalanceAsync(client.Id);
                    if (clientBalance <= 0m)
                        return RetailPosResult2.Fail<RetailPosReceiptDto2>("This client has no outstanding balance.");
                    if (amount > clientBalance)
                        return RetailPosResult2.Fail<RetailPosReceiptDto2>(
                            $"The payment of {amount:N2} is more than the {clientBalance:N2} the client owes. Advance payments are not supported.");
                }

                client.ModifiedOn = now;

                string prefix = RetailPosRules2.DocumentPrefix(RetailPosConstants2.ReceiptPrefix, paymentDate);
                int existing = await uow.Payments.CountPaymentsWithPrefixAsync(prefix);

                var payment = new RetailPosPayment2
                {
                    PaymentNumber = RetailPosRules2.BuildDocumentNumber(RetailPosConstants2.ReceiptPrefix, paymentDate, existing),
                    IdempotencyKey = request.IdempotencyKey,
                    ClientId = client.Id,
                    SaleId = request.SaleId,
                    Amount = amount,
                    PaymentDate = paymentDate,
                    Method = request.Method,
                    Reference = Clean(request.Reference, 100),
                    Remarks = Clean(request.Remarks, 250),
                    CreatedBy = userId,
                    CreatedOn = now
                };

                uow.Payments.AddPayment(payment);
                await uow.SaveChangesAsync();
                await uow.CommitAsync();

                var result = await BuildReceiptAsync(uow, payment.Id);
                return result == null
                    ? RetailPosResult2.Fail<RetailPosReceiptDto2>("Payment saved but the receipt could not be loaded.")
                    : RetailPosResult2.Ok(result, $"Payment {payment.PaymentNumber} saved.");
            }, _logRepo, nameof(ReceivePaymentAsync), _currentUser.UserId, request);
        }

        public Task<BaseResponse<RetailPosReceiptDto2>> GetReceiptAsync(int paymentId)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.RequireLogin<RetailPosReceiptDto2>(_currentUser);
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                var receipt = await BuildReceiptAsync(uow, paymentId);
                return receipt == null
                    ? RetailPosResult2.Fail<RetailPosReceiptDto2>("Payment not found.")
                    : RetailPosResult2.Ok(receipt);
            }, _logRepo, nameof(GetReceiptAsync), _currentUser.UserId, new { paymentId });
        }

        /// <summary>The balances shown are the balances at the moment the receipt is built.</summary>
        private async Task<RetailPosReceiptDto2?> BuildReceiptAsync(IRetailPosUnitOfWork2 uow, int paymentId)
        {
            var row = await uow.Payments.GetPaymentRowAsync(paymentId);
            if (row == null) return null;

            decimal invoiceBalance = 0m;
            if (row.SaleId.HasValue)
            {
                var balance = await uow.Sales.GetSaleBalanceAsync(row.SaleId.Value);
                invoiceBalance = balance?.OutstandingAmount ?? 0m;
            }

            decimal clientBalance = row.ClientId.HasValue ? await uow.Clients.GetBalanceAsync(row.ClientId.Value) : 0m;

            return new RetailPosReceiptDto2
            {
                PaymentId = row.Id,
                PaymentNumber = row.PaymentNumber,
                PaymentDate = row.PaymentDate,
                BusinessName = _currentUser.BusinessName,
                ClientName = string.IsNullOrEmpty(row.ClientName) ? RetailPosConstants2.WalkInName : row.ClientName,
                InvoiceNumber = row.InvoiceNumber,
                Amount = row.Amount,
                Method = row.Method,
                Reference = row.Reference,
                InvoiceBalanceAfter = invoiceBalance,
                ClientBalanceAfter = Math.Max(0m, clientBalance),
                ReceivedBy = row.ReceivedBy
            };
        }

        public Task<BaseResponse<List<RetailPosPaymentRowDto2>>> GetPaymentsAsync(RetailPosPaymentFilterDto2 filter)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.RequireLogin<List<RetailPosPaymentRowDto2>>(_currentUser);
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                return RetailPosResult2.Ok(await uow.Payments.GetPaymentRowsAsync(filter ?? new RetailPosPaymentFilterDto2()));
            }, _logRepo, nameof(GetPaymentsAsync), _currentUser.UserId, filter);
        }

        // ================================================================== reversal (owner only)

        public Task<BaseResponse<bool>> ReversePaymentAsync(RetailPosReversePaymentRequestDto2 request)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.RequireOwner<bool>(_currentUser, "reverse a payment");
                if (denied != null) return denied;

                string reason = (request.Reason ?? string.Empty).Trim();
                if (reason.Length < 3) return RetailPosResult2.Fail<bool>("Enter the reason for the reversal.");
                if (reason.Length > 250) return RetailPosResult2.Fail<bool>("The reason can be at most 250 characters.");

                int userId = _currentUser.UserId!.Value;
                DateTime now = DateTime.Now;

                await using var uow = _uowFactory.Create();
                await uow.BeginTransactionAsync();

                var payment = await uow.Payments.GetPaymentAsync(request.PaymentId);
                if (payment == null) return RetailPosResult2.Fail<bool>("Payment not found.");
                if (payment.IsReversed) return RetailPosResult2.Fail<bool>($"Payment {payment.PaymentNumber} is already reversed.");

                payment.IsReversed = true;
                payment.ReversedOn = now;
                payment.ReversedBy = userId;
                payment.ReversalReason = reason;

                // Touch the invoice and client so a parallel payment or credit sale cannot slip past the balance check.
                if (payment.SaleId.HasValue)
                {
                    var sale = await uow.Sales.GetSaleAsync(payment.SaleId.Value);
                    if (sale != null) sale.ModifiedOn = now;
                }
                if (payment.ClientId.HasValue)
                {
                    var client = await uow.Clients.GetClientAsync(payment.ClientId.Value);
                    if (client != null) client.ModifiedOn = now;
                }

                await uow.SaveChangesAsync();
                await uow.CommitAsync();
                return RetailPosResult2.Ok(true, $"Payment {payment.PaymentNumber} reversed.");
            }, _logRepo, nameof(ReversePaymentAsync), _currentUser.UserId, request);
        }

        // ================================================================== adjustments (owner only)

        public Task<BaseResponse<int>> AdjustBalanceAsync(RetailPosAdjustmentRequestDto2 request)
        {
            return RetailPosResult2.RunAsync(async () =>
            {
                var denied = RetailPosAccess2.RequireOwner<int>(_currentUser, "adjust a client's balance");
                if (denied != null) return denied;

                var errors = new List<string>();
                decimal amount = RetailPosRules2.Round(request.Amount);
                string reason = (request.Reason ?? string.Empty).Trim();

                if (request.ClientId <= 0) errors.Add("Select a client.");
                if (!RetailPosConstants2.AdjustmentTypes.Contains(request.AdjustmentType)) errors.Add("Choose a valid adjustment type.");
                if (amount <= 0m) errors.Add("The amount must be greater than zero.");
                if (amount > RetailPosConstants2.MaxAmount) errors.Add("The amount is too large.");
                if (reason.Length < 3) errors.Add("Enter the reason for the adjustment.");
                if (reason.Length > 250) errors.Add("The reason can be at most 250 characters.");
                if (errors.Count > 0) return RetailPosResult2.Fail<int>(errors);

                // A credit note always reduces what the client owes.
                bool decreases = request.AdjustmentType == RetailPosConstants2.AdjustmentCreditNote || !request.IncreasesBalance;

                int userId = _currentUser.UserId!.Value;
                DateTime now = DateTime.Now;

                await using var uow = _uowFactory.Create();
                await uow.BeginTransactionAsync();

                var client = await uow.Clients.GetClientAsync(request.ClientId);
                if (client == null) return RetailPosResult2.Fail<int>("Client not found.");

                if (request.SaleId.HasValue)
                {
                    var balance = await uow.Sales.GetSaleBalanceAsync(request.SaleId.Value);
                    if (balance == null) return RetailPosResult2.Fail<int>("Invoice not found.");
                    if (balance.ClientId != client.Id) return RetailPosResult2.Fail<int>("This invoice does not belong to the selected client.");
                    if (decreases && amount > balance.OutstandingAmount)
                        return RetailPosResult2.Fail<int>($"The adjustment of {amount:N2} is more than the {balance.OutstandingAmount:N2} outstanding on invoice {balance.InvoiceNumber}.");

                    var sale = await uow.Sales.GetSaleAsync(request.SaleId.Value);
                    if (sale != null) sale.ModifiedOn = now;
                }
                else if (decreases)
                {
                    decimal clientBalance = await uow.Clients.GetBalanceAsync(client.Id);
                    if (amount > clientBalance)
                        return RetailPosResult2.Fail<int>($"The adjustment of {amount:N2} is more than the {Math.Max(0m, clientBalance):N2} the client owes.");
                }

                client.ModifiedOn = now;

                var adjustment = new RetailPosAdjustment2
                {
                    ClientId = client.Id,
                    SaleId = request.SaleId,
                    AdjustmentType = request.AdjustmentType,
                    Amount = decreases ? -amount : amount,
                    Reason = reason,
                    CreatedBy = userId,
                    CreatedOn = now
                };

                uow.Payments.AddAdjustment(adjustment);
                await uow.SaveChangesAsync();
                await uow.CommitAsync();
                return RetailPosResult2.Ok(adjustment.Id, "Adjustment saved.");
            }, _logRepo, nameof(AdjustBalanceAsync), _currentUser.UserId, request);
        }
    }
}
