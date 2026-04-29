using AutoMapper;
using Azure.Core;
using Common;
using Common.Helpers;
using Common.Models;
using Common.Models.RequestModel;
using Common.Models.ResponseModel;
using Data.Repositories;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.Identity.Client;
using Service.IService;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepo _transactionRepo;
        private readonly IProductRepo _productRepo;
        private readonly IMapper _mapper;
        private readonly ILogRepository _logRepo;
        private readonly IClientRepository _clientRepo;
        private readonly ISlipPrinter _slipPrinter;
        private readonly IStockRepo _stockRepo;
        public TransactionService(
            ITransactionRepo transactionRepo,
            IMapper mapper,
            ILogRepository logRepo,
            ISlipPrinter slipPrinter,
            IProductRepo productRepo,
            IClientRepository clientRepo,
            IStockRepo stockRepo)
        {
            _transactionRepo = transactionRepo;
            _mapper = mapper;
            _logRepo = logRepo;
            _slipPrinter = slipPrinter;
            _productRepo = productRepo;
            _clientRepo = clientRepo;
            _stockRepo = stockRepo;
        }

        private async Task<BaseResponse<AddEditTransactionResponseModel>> ValidateTransactionAsync
            (AddEditTransactionRequestModel request)
        {
            if (!request.ProductId.HasValue || request.ProductId == 0)
                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { "Product required" },
                    "Error occurred "
                );
            if (!request.Discount.HasValue || (request.Discount == 0 || request.Discount == decimal.Zero))
                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { "Client amount required" },
                    "Error occurred "
                );
            if (!request.Quantity.HasValue || request.Quantity == 0)
                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { "Quantity required" },
                    "Error occurred "
                );
            if (!request.UnitPrice.HasValue || (request.UnitPrice == 0 || request.UnitPrice == decimal.Zero))
                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { "Unit price required" },
                    "Error occurred "
                );
            if (string.IsNullOrWhiteSpace(request.TransactionType) || string.IsNullOrEmpty(request.TransactionType))
                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { "Transaction type required" },
                    "Error occurred "
                );
            if (!new string[] { "Sell", "Purchase" }.Contains(request.TransactionType))
                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { "Invalid transaction type" },
                    "Error occurred "
                );
            if (!request.TransactionDate.HasValue)
                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { "Transaction date required" },
                    "Error occurred "
                );
            if (request.TransactionDate > DateTime.Now)
                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { "Transaction date cannot be in the future" },
                    "Error occurred "
                );

            var getProduct = await _productRepo.GetProductsAsync(request.ProductId.Value, 1, 1, string.Empty);
            if (getProduct.Item1 == null || getProduct.Item2 == 0 || getProduct.Item1.Count == 0)
                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { "Product not found" },
                    "Error occurred "
                );

            int currentStock = getProduct.Item1[0].Stocks.Sum(s => s.Quantity ?? 0);
            if (currentStock < request.Quantity)
                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { "Insufficient stock" },
                    "Error occurred "
                );

            return BaseResponse<AddEditTransactionResponseModel>.SuccessResponse(
                new AddEditTransactionResponseModel(),
                "Transaction processed successfully"
            );
        }
        public async Task PrintSlipAsync(string slipContent)
        {
            try
            {
                _slipPrinter.PrintSlip(slipContent);
            }
            catch (Exception ex)
            {
                // Log error
                await _logRepo.LogExceptionAsync(ex, userId: null, additionalData: "{ \"message\": error while printing slip }");
                throw; // Re-throw exception after logging
            }
        }
        private async Task<int?> AnaylyzeAndAddClient(string? name, string? address, string? phone)
        {
            int? clientId = null;
            try
            {

                var clients = await _clientRepo.GetAllClientsAsync(0, 1, int.MaxValue, $"x=> x.Phone == {phone}");
                if (clients.Item1 != null && clients.Item1.Count > 0)
                {
                    var selectedClient = clients.Item1
                    .FirstOrDefault(x => !string.IsNullOrEmpty(x.FirstName)
                    && !string.IsNullOrEmpty(name)
                    && x.FirstName.StartsWith(name.Substring(0, Math.Min(3, name.Length)),
                    StringComparison.OrdinalIgnoreCase));

                    if (selectedClient != null)
                        clientId = selectedClient?.Id;
                    else
                    {
                        var newClient = new AddEditClientDetailRequestModel
                        {
                            FirstName = name,
                            Phone = phone,
                            Address = address
                        };
                        var addedClient = await _clientRepo.AddEditClientAsync(newClient);
                        if (addedClient.Success)
                            clientId = addedClient.Data;
                    }
                }
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(ex, userId: null, additionalData: "{ \"message\": error in AnaylyzeAndAddClient}");
            }
            return clientId;
        }

        public async Task<BaseResponse<int?>> ProcessTransactions(ProcessTransactionsModel transactionModel)
        {
            try
            {
                if (transactionModel == null)
                    return BaseResponse<int?>.FailureResponse(
                        new List<string> { "model is null" },
                        "Error occurred "
                    );


                var transactionEntity = new TransactionMst
                {
                    Id= transactionModel.TransactionId??0,
                    ClientAddress = transactionModel.ClientAddress,
                    ClientId = transactionModel.ClientId,
                    ClientName = transactionModel.ClientName,
                    TransactionType = transactionModel.TransactionType,
                    TransactionNumber = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff")),
                    TransactionDate = transactionModel.TransactionDate ?? DateTime.Now,
                    IsActive= true,
                    Remarks = transactionModel.Remarks,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = transactionModel.CreatedBy,
                    TransactionDetails = transactionModel?.Products?.Select(td => new TransactionDetails
                    {
                        Id=td.Id,
                        ProductId = td.ProductId,
                        Quantity = td.Quantity,
                        UnitPrice = td.UnitPrice,
                        Discount = td.Discount,
                        CreatedOn = DateTime.UtcNow,
                        CreatedBy = transactionModel.CreatedBy
                    }).ToList()
                };


                if (!string.IsNullOrEmpty(transactionModel.ClientPhone)
                    && !string.IsNullOrEmpty(transactionModel.ClientName)
                    && (!transactionModel.ClientId.HasValue || transactionModel.ClientId == 0))
                {
                    var analyzeResult = await AnaylyzeAndAddClient(
                        transactionModel.ClientName, transactionModel.ClientPhone, transactionModel.ClientAddress);
                    transactionEntity.ClientId = analyzeResult;
                }
                if (!transactionModel.ClientId.HasValue || transactionModel.ClientId == 0)
                {
                    transactionEntity.ClientName = transactionModel.ClientName;
                    transactionEntity.ClientAddress = transactionModel.ClientAddress;
                }
                BaseResponse<int?> response = await _transactionRepo.ProcessTransactions(transactionEntity);
                if (response.Success)

                {
                    if (!transactionModel.TransactionId.HasValue || transactionModel.TransactionId==0)
                    {
                        transactionModel.TransactionDate = transactionModel.TransactionDate ?? transactionEntity.TransactionDate;
                        // Generate slip
                        string slip = GenerateSlip(transactionModel, transactionModel.ClientName, transactionEntity.TransactionNumber.ToString());

                        await _transactionRepo.AddEditTransactionSlipAsync(new TransactionSlip
                        {
                            TransactionId = transactionEntity.Id,
                            SlipContent = slip
                        });

                        await PrintSlipAsync(slip);
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(ex, userId: null, additionalData: "{ \"message\": error ProcessTransactions}");

                return BaseResponse<int?>.FailureResponse(
                    new List<string> { ex.Message },
                    "Error occurred "
                );
            }
        }
        private string GenerateSlip(ProcessTransactionsModel transaction, string? clientName, string? transactionNumber)
        {
            var sb = new StringBuilder();
            sb.AppendLine("========== Transaction Slip ==========");
            sb.AppendLine($"Slip No: {transactionNumber}");
            sb.AppendLine($"Date   : {transaction.TransactionDate?.ToString("dd-MMM-yyyy HH:mm")}");
            sb.AppendLine($"Client : {clientName}");
            sb.AppendLine($"Type   : {transaction.TransactionType}");
            sb.AppendLine("-----------Product details------------");
            transaction.Products
            .Select((x, index) => new { x, index })
            .ToList()
             .ForEach(item =>
             {
                 sb.AppendLine($"{item.index + 1}. Product: {item.x?.Name}");
                 sb.AppendLine($"    Quantity  : {item.x?.Quantity}");
                 sb.AppendLine($"    Unit Price: {item.x.UnitPrice:C}");
                 sb.AppendLine($"    Total     : {(item.x.Quantity * (item.x.UnitPrice)):C}");
             });
            sb.AppendLine($"Discount  : {transaction.TotalDiscount ?? 0:C}");
            sb.AppendLine($"Total     : {transaction.TotalAmount ?? 0:C}");
            sb.AppendLine($"Net Amount: {transaction.NetAmount:C}");
            sb.AppendLine("--------------------------------------");
            sb.AppendLine($"Remarks: {transaction.Remarks}");
            sb.AppendLine("======================================");

            return sb.ToString();
        }
        public async Task<PagedTransactionResponse> GetTransactionsAsync(TransactionFilterRequest request)
        {
            return await _transactionRepo.GetTransactionsAsync(request);
        }
        public async Task<ProcessTransactionsModel> GetTransactionByIdAsync(int transactionId)
        {
            var getTransaction = await _transactionRepo.GetTransactionByIdAsync(transactionId);
            if (getTransaction == null)
                return null;

            // Map to your ProcessTransactionsModel
            var model = new ProcessTransactionsModel
            {
                TransactionId=getTransaction.Id,
                ClientId = getTransaction.ClientId,
                ClientName = getTransaction.ClientName,
                ClientAddress = getTransaction.ClientAddress,
                TransactionType = getTransaction.TransactionType,
                TransactionDate = getTransaction.TransactionDate,
                Remarks = getTransaction.Remarks,
                TransactionNumber = getTransaction.TransactionNumber,
                Products = getTransaction.TransactionDetails.Select(d => new ProductWidgetModel
                {
                    Id = d.Id,
                    ProductId = d.ProductId,
                    Name = d.Product.Name,
                    Quantity = d.Quantity-(d.ReturnedQuantity??0),
                    UnitPrice = d.UnitPrice.Value,
                    Discount = d.Discount
                }).ToList()
            };

            return model;
        }
        //public async Task<BaseResponse<int?>> CreateReturnTransaction(ReturnRequestModel request)
        //{
        //    try
        //    {
        //        if (request == null || request.ReturnProducts == null || !request.ReturnProducts.Any())
        //            return BaseResponse<int?>.FailureResponse(new List<string> { "Invalid request" }, "Error");
        //        StringBuilder stringBuilder = new StringBuilder();
        //        foreach (var returnItem in request.ReturnProducts)
        //        {
        //            if (returnItem.Quantity < 1)
        //            {
        //                stringBuilder.AppendLine($"Return quantity for product {returnItem.ProductId} cannot be negative or zero.");
        //                continue;
        //            }
        //            var getTransaction = await _transactionRepo.GetTransactionDetailByProduct(request.TransactionId, returnItem.ProductId);
        //            if (getTransaction == null)
        //            {
        //                stringBuilder.AppendLine($"Product {returnItem.ProductId} not found.");
        //                continue;
        //            }

        //            int requestedReturnQuantity = returnItem.Quantity == 0 ? getTransaction.Quantity : returnItem.Quantity;
        //            //if (getTransaction?.Quantity == returnItem.Quantity)
        //            //    continue;

        //            int alreadyReturned = getTransaction.ReturnedQuantity ?? 0;
        //            int availableQty = getTransaction.Quantity - alreadyReturned;

        //            if (returnItem.Quantity > availableQty)
        //            {
        //                stringBuilder.AppendLine($"Only {availableQty} available for return.");
        //                continue;
        //            }

        //            decimal unitPrice = getTransaction.UnitPrice ?? 0;
        //            decimal discount = getTransaction.Discount ?? 0;
        //            decimal amount = (unitPrice * requestedReturnQuantity) - discount;

        //            int stockQty = getTransaction.TransMst.TransactionType == "Sell"
        //                ? returnItem.Quantity
        //                : -returnItem.Quantity;

        //            // 🔁 Call data layer
        //            var resp = await _transactionRepo.CreateReturnTransaction(
        //                request.TransactionId,
        //                returnItem.ProductId,
        //                returnItem.Quantity==0?getTransaction.Quantity:returnItem.Quantity,
        //                amount,
        //                stockQty,
        //                request.CreatedBy??0
        //            );

        //            if (!resp.Success)
        //            {
        //                stringBuilder.AppendLine(string.Join(", ", resp.Errors));
        //                continue;
        //            }
        //        }

        //        if (stringBuilder.Length > 0)
        //        {
        //            return BaseResponse<int?>.FailureResponse(new List<string> { stringBuilder.ToString() }, "Error occurred");
        //        }

        //        return BaseResponse<int?>.SuccessResponse(request.TransactionId, "Return successful");
        //    }
        //    catch (Exception ex)
        //    {
        //        return BaseResponse<int?>.FailureResponse(new List<string> { ex.Message }, "Error occurred");
        //    }
        //}
        public async Task<BaseResponse<List<PreparedReturnProduct>>> ValidateReturnRequest(ReturnRequestModel request)
        {
            if (request == null || request.ReturnProducts == null || !request.ReturnProducts.Any())
                return BaseResponse<List<PreparedReturnProduct>>.FailureResponse(
                    new List<string> { "Invalid request" }, "Error");

            var errors = new List<string>();
            var preparedList = new List<PreparedReturnProduct>();

            foreach (var returnItem in request.ReturnProducts)
            {
                if (returnItem.Quantity < 1)
                {
                    errors.Add($"Return quantity for product {returnItem.ProductId} cannot be negative or zero.");
                    continue;
                }

                var getTransaction = await _transactionRepo.GetTransactionDetailByProduct(request.TransactionId, returnItem.ProductId);
                if (getTransaction == null)
                {
                    errors.Add($"Product {returnItem.ProductId} not found.");
                    continue;
                }

                int alreadyReturned = getTransaction.ReturnedQuantity ?? 0;
                int availableQty = getTransaction.Quantity - alreadyReturned;

                if (returnItem.Quantity > availableQty)
                {
                    errors.Add($"Only {availableQty} available for return for product {returnItem.ProductId}.");
                    continue;
                }

                // ✅ Correct discount calculation (percentage)
                decimal unitPrice = getTransaction.UnitPrice ?? 0;
                decimal discountPercent = getTransaction.Discount ?? 0;
                decimal grossAmount = unitPrice * returnItem.Quantity;
                decimal discountAmount = grossAmount * (discountPercent / 100);
                decimal amount = grossAmount - discountAmount;

                int stockQty = getTransaction.TransMst.TransactionType == "Sell"
                    ? returnItem.Quantity
                    : -returnItem.Quantity;

                preparedList.Add(new PreparedReturnProduct
                {
                    TransactionId = request.TransactionId,
                    ProductId = returnItem.ProductId,
                    Quantity = returnItem.Quantity,
                    Amount = unitPrice,
                    Discount = discountPercent,
                    //DiscountAmount = discountAmount,
                    StockQty = stockQty,
                    CreatedBy = request.CreatedBy ?? 0
                });
            }

            if (errors.Any())
                return BaseResponse<List<PreparedReturnProduct>>.FailureResponse(errors, "Validation errors");

            return BaseResponse<List<PreparedReturnProduct>>.SuccessResponse(preparedList, "Validation successful");
        }

        public async Task<BaseResponse<int?>> CreateReturnTransaction(ProcessTransactionsModel request)
        {
            // Step 1: Validate and prepare
            var validationResult = await ValidateReturnRequest(
                new ReturnRequestModel {TransactionId= request.TransactionId.Value, ReturnProducts = request.Products
                .Select(p => new ReturnProductModel { ProductId = p.ProductId, Quantity = p.Quantity }).ToList() });

            if (!validationResult.Success)
                return BaseResponse<int?>.FailureResponse(validationResult.Errors, "Validation failed");

            var preparedList = validationResult.Data;

            // Step 2: Build TransactionMst for Return
            var transactionEntity = new TransactionMst
            {
                ClientId = request.ClientId,
                ClientName = request.ClientName,
                ClientAddress = request.ClientAddress,
                TransactionType = "Return", // 🔑
                TransactionNumber = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff")),
                ReferenceNumber = request.TransactionNumber, // 🔑 link to original
                TransactionDate = DateTime.Now,
                IsActive = true,
                Remarks = request.Remarks,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = request.CreatedBy,
                TransactionDetails = preparedList.Select(p => new TransactionDetails
                {
                    ProductId = p.ProductId,
                    Quantity = p.Quantity,
                    UnitPrice = p.Amount / p.Quantity, // net unit price after discount
                    Discount = p.Discount,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = request.CreatedBy
                }).ToList()
            };

            // Step 3: Persist
            return await _transactionRepo.ProcessTransactions(transactionEntity);
        }

        public async Task<BaseResponse<bool>> RevertTransaction(int transactionId)
        {
            try
            {
                // Call repository to revert
                BaseResponse<bool> response = await _transactionRepo.RevertTransaction(transactionId);

                return response;
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(ex, userId: null,
                    additionalData: "{ \"message\": \"Error RevertTransaction\", \"TransactionId\": " + transactionId + "}");

                return BaseResponse<bool>.FailureResponse(
                    new List<string> { ex.Message },
                    "Revert failed"
                );
            }
        }
        public async Task<BaseResponse<DashboardResponse>> GetDashboardAsync(DateTime? from, DateTime? to)
        {
            try
            {
                var summary = await _transactionRepo.GetSummaryAsync(from, to);
                var recent = await _transactionRepo.GetRecentTransactionsAsync(10);

                var response = new DashboardResponse
                {
                    Summary = summary,
                    RecentTransactions = recent
                };

                return BaseResponse<DashboardResponse>.SuccessResponse(response, "Success");
            }
            catch (Exception ex)
            {
                return BaseResponse<DashboardResponse>.FailureResponse(
                    new List<string> { ex.Message }, "Error");
            }
        }
    }
}
