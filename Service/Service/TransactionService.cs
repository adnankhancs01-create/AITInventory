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
            if(!request.ProductId.HasValue || request.ProductId==0)
                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { "Product required" },
                    "Error occurred "
                );
            //if((!request.ClientId.HasValue || request.ClientId == 0 )&&string.IsNullOrEmpty(request.ClientName))
            //    return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
            //        new List<string> { "Client required" },
            //        "Error occurred "
            //    );
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
            if (!new string[] { "Sell","Purchase"}.Contains(request.TransactionType))
                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { "Invalid transaction type" },
                    "Error occurred "
                );
            if(!request.TransactionDate.HasValue)
                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { "Transaction date required" },
                    "Error occurred "
                );
            if(request.TransactionDate>DateTime.Now)
                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { "Transaction date cannot be in the future" },
                    "Error occurred "
                );

            var getProduct=await _productRepo.GetProductsAsync(request.ProductId.Value, 1, 1, string.Empty);
            if(getProduct.Item1 == null || getProduct.Item2==0|| getProduct.Item1.Count==0)
                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { "Product not found" },
                    "Error occurred "
                );

            int currentStock = getProduct.Item1[0].Stocks.Sum(s => s.Quantity??0);
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

        //public async Task<BaseResponse<AddEditTransactionResponseModel>> AddEditTransactionAsync(AddEditTransactionRequestModel request)
        //{
        //    try
        //    {
        //        var validate=await ValidateTransactionAsync(request);
        //        if(!validate.Success)
        //            return validate;

        //        var transactionEntity = new VendorTransaction();
        //        transactionEntity.Id = request.Id;
        //        transactionEntity.ClientId = request.ClientId;
        //        transactionEntity.ProductId = request.ProductId;
        //        transactionEntity.Quantity = request.Quantity;
        //        transactionEntity.UnitPrice = request.UnitPrice;
        //        transactionEntity.ClientAmount = transactionEntity.ClientAmount;

        //        if (!string.IsNullOrEmpty(request.ClientPhone) 
        //            && !string.IsNullOrEmpty(request.ClientName)
        //            && (!request.ClientId.HasValue || request.ClientId == 0 ))
        //        { 
        //            var analyzeResult = await AnaylyzeAndAddClient(
        //                request.ClientName, request.ClientPhone, request.ClientAddress);
        //            transactionEntity.ClientId = analyzeResult;
        //        }
        //        if (!request.ClientId.HasValue || request.ClientId == 0)
        //        {
        //            transactionEntity.ClientName = request.ClientName;
        //            transactionEntity.ClientAddress = request.ClientAddress;
        //        }
        //        transactionEntity.Remarks = request.Remarks;
        //        transactionEntity.TransactionType = request.TransactionType;
        //        transactionEntity.TransactionDate ??= DateTime.Now;
        //        transactionEntity.TransactionNumber = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff"));

        //        // Save transaction
        //        string slip = string.Empty;
        //        var savedTransaction = await _transactionRepo.AddEditTransactionAsync(transactionEntity);
        //        if (savedTransaction.Id > 0)
        //        {
        //            var stockDeductionResult=await _stockRepo.DeductStockAsync(request.ProductId.Value, request.Quantity.Value);
        //            // Generate slip
        //            slip = _transactionRepo.GenerateSlip(savedTransaction, request.ClientName);

        //            await _transactionRepo.AddEditTransactionSlipAsync(new TransactionSlip
        //            {
        //                TransactionId = savedTransaction.Id,
        //                SlipContent = slip
        //            });

        //            //await PrintSlipAsync(slip);
        //        }

        //        // Map entity back to response model
        //        var responseModel = new AddEditTransactionResponseModel();
        //        responseModel.Slip = slip;
        //        responseModel.TransactionId = savedTransaction.Id;

        //        return BaseResponse<AddEditTransactionResponseModel>.SuccessResponse(
        //            responseModel,
        //            "Transaction processed successfully"
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log error
        //        await _logRepo.LogExceptionAsync(ex, userId: null, additionalData: "{ \"message\": error while fetching }");

        //        return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
        //            new List<string> { ex.Message },
        //            "Error occurred "
        //        );
        //    }
        //}
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
        //public async Task<BaseResponse<(List<GetTransactionResponseModel>, int)>> GetTransactionAsync(int? transactionId, int pageIndex, int pageSize, string? Filter)
        //{
        //    try
        //    {
        //        (List<VendorTransaction>,int) getTransactions = await _transactionRepo.GetTransactionAsync(transactionId, pageIndex, pageSize, Filter);

        //        var result = getTransactions.Item1.Select(t => new GetTransactionResponseModel
        //        {

        //            ClientName = (t.Client == null)
        //    ? null
        //    : $"{t.Client.FirstName} {t.Client.LastName}".Trim(),
        //            ClientPhone = t.Client?.Phone,
        //            ClientAddress = t.Client?.Address,
        //            ProductName=t.Product?.Name,
        //            Id = t.Id,
        //            ClientId = t.ClientId,
        //            ProductId = t.ProductId,
        //            Quantity = t.Quantity,
        //            UnitPrice = t.UnitPrice,
        //            Discount = t.ClientAmount,
        //            Remarks = t.Remarks,
        //            TransactionType = t.TransactionType,
        //            TransactionDate =t.TransactionDate
        //        }).ToList();
        //        if (result == null || result.Count == 0)
        //            return new BaseResponse<(List<GetTransactionResponseModel>, int)>(
        //                new List<string> { "Transactions not found" }, "Error fetching transactions");

        //        return BaseResponse<(List<GetTransactionResponseModel>, int)>.SuccessResponse(
        //            (result, getTransactions.Item2),
        //            "Transaction retrieved successfully"
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        await _logRepo.LogExceptionAsync(ex, userId: null, additionalData: "{ \"message\": error while fetching }");

        //        return BaseResponse<(List<GetTransactionResponseModel>, int)>.FailureResponse(
        //            new List<string> { ex.Message },
        //            "Error occurred "
        //        );
        //    }

        //}
        private async Task<int?> AnaylyzeAndAddClient(string? name, string? address, string? phone)
        {
            int? clientId=null;
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
                    else {             
                        var newClient = new AddEditClientDetailRequestModel
                        {
                            FirstName = name,
                            Phone = phone,
                            Address = address
                        };
                        var addedClient = await _clientRepo.AddEditClientAsync(newClient);
                        if(addedClient.Success)
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
                    ClientAddress = transactionModel.ClientAddress,
                    ClientId = transactionModel.ClientId,
                    ClientName = transactionModel.ClientName,
                    TransactionType = transactionModel.TransactionType,
                    TotalDiscount = transactionModel.TotalDiscount,
                    TotalAmount = transactionModel.TotalAmount,
                    NetAmount = transactionModel.NetAmount,
                    TransactionNumber = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff")),
                    TransactionDate = transactionModel.TransactionDate?? DateTime.Now,
                    Remarks = transactionModel.Remarks,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = transactionModel.CreatedBy,
                    TransactionDetails = transactionModel?.Products?.Select(td => new TransactionDetails
                    {
                        ProductId = td.ProductId,
                        Quantity = td.Quantity,
                        UnitPrice = td.UnitPrice,
                        Discount=td.Discount,
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
                    transactionModel.TransactionDate= transactionModel.TransactionDate ?? transactionEntity.TransactionDate;
                    // Generate slip
                    string slip = GenerateSlip(transactionModel, transactionModel.ClientName, transactionEntity.TransactionNumber.ToString());

                    await _transactionRepo.AddEditTransactionSlipAsync(new TransactionSlip
                    {
                        TransactionId = transactionEntity.Id,
                        SlipContent = slip
                    });

                    await PrintSlipAsync(slip);
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
    }
}
