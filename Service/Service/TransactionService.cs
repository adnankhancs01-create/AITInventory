using AutoMapper;
using Azure.Core;
using Common;
using Common.Helpers;
using Common.Models;
using Common.Models.RequestModel;
using Common.Models.ResponseModel;
using Data.Migrations;
using Data.Repositories;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.Identity.Client;
using Service.IService;
using System;
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
        public TransactionService(
            ITransactionRepo transactionRepo,
            IMapper mapper,
            ILogRepository logRepo,
            ISlipPrinter slipPrinter,
            IProductRepo productRepo,
            IClientRepository clientRepo)
        {
            _transactionRepo = transactionRepo;
            _mapper = mapper;
            _logRepo = logRepo;
            _slipPrinter = slipPrinter;
            _productRepo = productRepo;
            _clientRepo = clientRepo;
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
            if (!request.ClientAmount.HasValue || (request.ClientAmount == 0 || request.ClientAmount == decimal.Zero))
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

        public async Task<BaseResponse<AddEditTransactionResponseModel>> AddEditTransactionAsync(AddEditTransactionRequestModel request)
        {
            try
            {
                var validate=await ValidateTransactionAsync(request);
                if(!validate.Success)
                    return validate;

                var transactionEntity = new VendorTransaction();
                transactionEntity.Id = request.Id;
                transactionEntity.ClientId = request.ClientId;
                transactionEntity.ProductId = request.ProductId;
                transactionEntity.Quantity = request.Quantity;
                transactionEntity.UnitPrice = request.UnitPrice;
                transactionEntity.ClientAmount = transactionEntity.ClientAmount;

                if (!string.IsNullOrEmpty(request.ClientPhone) 
                    && !string.IsNullOrEmpty(request.ClientName)
                    && (!request.ClientId.HasValue || request.ClientId == 0 ))
                { 
                    var analyzeResult = await AnaylyzeAndAddClient(
                        request.ClientName, request.ClientPhone, request.ClientAddress);
                    transactionEntity.ClientId = analyzeResult;
                }
                if (!request.ClientId.HasValue || request.ClientId == 0)
                {
                    transactionEntity.ClientName = request.ClientName;
                    transactionEntity.ClientAddress = request.ClientAddress;
                }
                transactionEntity.Remarks = request.Remarks;
                transactionEntity.TransactionType = request.TransactionType;
                transactionEntity.TransactionDate ??= DateTime.Now;
                transactionEntity.TransactionNumber = Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff"));

                // Save transaction
                var savedTransaction = await _transactionRepo.AddEditTransactionAsync(transactionEntity);

                // Generate slip
                var slip = _transactionRepo.GenerateSlip(savedTransaction, request.ClientName);

                // Map entity back to response model
                var responseModel = new AddEditTransactionResponseModel();
                responseModel.Slip = slip;
                responseModel.TransactionId = savedTransaction.Id;

                await _transactionRepo.AddEditTransactionSlipAsync(new TransactionSlip
                {
                    TransactionId = savedTransaction.Id,
                    SlipContent = slip
                });

                //await PrintSlipAsync(slip);

                return BaseResponse<AddEditTransactionResponseModel>.SuccessResponse(
                    responseModel,
                    "Transaction processed successfully"
                );
            }
            catch (Exception ex)
            {
                // Log error
                await _logRepo.LogExceptionAsync(ex, userId: null, additionalData: "{ \"message\": error while fetching }");

                return BaseResponse<AddEditTransactionResponseModel>.FailureResponse(
                    new List<string> { ex.Message },
                    "Error occurred "
                );
            }
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
        public async Task<BaseResponse<(List<GetTransactionResponseModel>, int)>> GetTransactionAsync(int? transactionId, int pageIndex, int pageSize, string? Filter)
        {
            try
            {
                (List<VendorTransaction>,int) getTransactions = await _transactionRepo.GetTransactionAsync(transactionId, pageIndex, pageSize, Filter);

                var result = getTransactions.Item1.Select(t => new GetTransactionResponseModel
                {

                    ClientName = (t.Client == null)
            ? null
            : $"{t.Client.FirstName} {t.Client.LastName}".Trim(),
                    ClientPhone = t.Client?.Phone,
                    ClientAddress = t.Client?.Address,
                    ProductName=t.Product?.Name,
                    Id = t.Id,
                    ClientId = t.ClientId,
                    ProductId = t.ProductId,
                    Quantity = t.Quantity,
                    UnitPrice = t.UnitPrice,
                    ClientAmount = t.ClientAmount,
                    Remarks = t.Remarks,
                    TransactionType = t.TransactionType,
                    TransactionDate =t.TransactionDate
                }).ToList();
                if (result == null || result.Count == 0)
                    return new BaseResponse<(List<GetTransactionResponseModel>, int)>(
                        new List<string> { "Transactions not found" }, "Error fetching transactions");

                return BaseResponse<(List<GetTransactionResponseModel>, int)>.SuccessResponse(
                    (result, getTransactions.Item2),
                    "Transaction retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(ex, userId: null, additionalData: "{ \"message\": error while fetching }");

                return BaseResponse<(List<GetTransactionResponseModel>, int)>.FailureResponse(
                    new List<string> { ex.Message },
                    "Error occurred "
                );
            }

        }
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
    }
}
