using AutoMapper;
using Common;
using Common.Models;
using Common.Models.RequestModel;
using Common.Models.ResponseModel;
using Domain.Entities;
using Domain.IRepositories;
using Service.IService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace Service.Service
{
    public class StockService:IStockService
    {
        private readonly IStockRepo _stockRepo;
        private readonly IMapper _mapper;
        private readonly ILogRepository _logRepo;
        public StockService(
            IStockRepo stockRepo,
            IMapper mapper,
            ILogRepository logRepo)
        {
            _stockRepo = stockRepo;
            _mapper = mapper;
            _logRepo = logRepo;
        }

        public async Task<BaseResponse<VendorStockModel>> AddEditVendorStockAsync(AddEditVendorStockRequestModel requestModel)
        {
            try
            {
                var stockResult = await _stockRepo.AddEditVendorStockAsync(new VendorStock
                {
                    ProductId = requestModel.ProductId,
                    Quantity = requestModel.Quantity,
                    StockNumber = requestModel.StockNumber,
                    TotalPurchasePrice = requestModel.TotalPurchasePrice
                });
                if (!stockResult.Success)
                    return new BaseResponse<VendorStockModel>(
                        new List<string> { "Error" }, "Something went wrong");

                var data = _mapper.Map<VendorStock, VendorStockModel>(stockResult.Data);
                return BaseResponse<VendorStockModel>.SuccessResponse(data, "Request executed successfully");
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(
                    ex,
                    userId: requestModel.UserId,
                    additionalData: JsonSerializer.Serialize(new { message = "Error while adding or updating Stock" }),
                    request: JsonSerializer.Serialize(requestModel)
                );

                return BaseResponse<VendorStockModel>.FailureResponse(
                    new() { ex.Message },
                    "Error occurred while processing request"
                );
            }
        }
        public async Task<BaseResponse<(List<GetStockResponseModel>, int)>> GetStocksReportAsync(int? productId, int pageIndex, int pageSize, string? Filter)
        {
            try
            {
                (List<VendorStock>, int) getStocks = await _stockRepo.GetStockAsync(productId, pageIndex, pageSize, Filter);
                return new BaseResponse<(List<GetStockResponseModel>, int)>((getStocks.Item1.Select(s => new GetStockResponseModel
                {
                    Id = s.Id,
                    ProductId = s.ProductId,
                    ProductName = s.Product?.Name,
                    ProductCode = s.Product?.ProductCode,
                    Category = s.Product?.Category?.Name,
                    StockNumber = s.StockNumber,
                    Quantity = s.Quantity,
                    TotalPurchasePrice = s.TotalPurchasePrice,
                    CreatedOn = s.CreatedOn,
                    CreatedBy = s.CreatedBy,
                    ModifiedOn = s.ModifiedOn,
                    ModifiedBy = s.ModifiedBy
                }).ToList(), getStocks.Item2), string.Empty);
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(ex, userId: null, additionalData: "{ \"message\": error while fetching }");

                return new BaseResponse<(List<GetStockResponseModel>, int)>(
                        new List<string> { ex.Message }, "Error fetching stocks");
            }

        }
    }
}
