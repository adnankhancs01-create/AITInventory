using AutoMapper;
using Common;
using Common.Models;
using Common.Models.RequestModel;
using Common.Models.ResponseModel;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.IdentityModel.Tokens;
using Service.IService;
using System.Text.Json;

namespace Service.Service
{
    public class ProductService : IProductService
    {
        private readonly IProductRepo _productRepo;
        private readonly IMapper _mapper;
        private readonly ILogRepository _logRepo;
        public ProductService(
            IProductRepo productRepo,
            IMapper mapper,
            ILogRepository logRepo)
        {
            _productRepo = productRepo;
            _mapper = mapper;
            _logRepo = logRepo;
        }

        public async Task<BaseResponse<(List<ProductModel>, int)>> GetProductsAsync(int productId, int pageIndex, int pageSize, string? Filter)
        {
            try
            {
                var getProduct = await _productRepo.GetProductsAsync(productId, pageIndex, pageSize, Filter);
                if (getProduct.Item1 == null)
                    return new BaseResponse<(List<ProductModel>, int)>(
                        new List<string> { "Product not found" }, "Error fetching product");

                var data = _mapper.Map<List<Product>, List<ProductModel>>(getProduct.Item1);
                return new BaseResponse<(List<ProductModel>, int)>((data, getProduct.Item2), string.Empty);
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(ex, userId: null, additionalData: "{ \"message\": error while fetching }");

                return new BaseResponse<(List<ProductModel>, int)>(
                        new List<string> { ex.Message }, "Error fetching product");
            }
        }

        public async Task<BaseResponse<(List<ProductCategoryModel>, int)>> GetProductCategoriesAsync(int id, int pageIndex, int pageSize, string? Filter)
        {
            try
            {
                var getProduct = await _productRepo.GetProductCategoriesAsync(id, pageIndex, pageSize, Filter);
                if (getProduct.Item1==null)
                    return new BaseResponse<(List<ProductCategoryModel>, int)>(
                        new List<string> { "Product not found" }, "Error fetching product");

                var data = _mapper.Map<List<ProductCategory>, List<ProductCategoryModel>>(getProduct.Item1);
                return new BaseResponse<(List<ProductCategoryModel>, int)>((data, getProduct.Item2), string.Empty);
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(ex, userId: null, additionalData: "{ \"message\": error while fetching }");

                return new BaseResponse<(List<ProductCategoryModel>, int)>(
                        new List<string> { ex.Message }, "Error fetching product");
            }
        }

        public async Task<BaseResponse<ProductModel>> AddEditProductAsync(ProductRequesModel requestModel)
        {
            try
            {
                long? stockNumber = null;
                var request = _mapper.Map<Product>(requestModel);
                if (request == null)
                    return new BaseResponse<ProductModel>(
                        new List<string> { "Error" }, "Something went wrong");
                
                if(requestModel.Id.HasValue)
                    request.ModifiedOn = DateTime.Now;
                else
                    request.CreatedOn = DateTime.Now;
                
                var result= await _productRepo.AddEditProductAsync(request);
                if (result.Success && 
                    (requestModel.Quantity.HasValue || requestModel.TotalPurchasePrice.HasValue))
                {
                    var stockResult=await _productRepo.AddEditVendorStockAsync(new VendorStock { 
                        ProductId=result.Data.Id,
                        Quantity=requestModel.Quantity,
                        StockNumber=requestModel.StockNumber,
                        TotalPurchasePrice=requestModel.TotalPurchasePrice
                    });
                    stockNumber = stockResult?.Data?.StockNumber;
                }

                if (result.Success && (requestModel.UnitPrice.HasValue))
                    await _productRepo.AddPricingAsync(new Pricing
                    {
                        ProductCode = result.Data.ProductCode,
                        UnitPrice = requestModel.UnitPrice
                    });

                return BaseResponse<ProductModel>.SuccessResponse(_mapper.Map<Product, ProductModel>(result.Data));
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(
                    ex,
                    userId: requestModel.UserId,
                    additionalData: JsonSerializer.Serialize(new { message = "Error while adding or updating product" }),
                    request: JsonSerializer.Serialize(requestModel)
                );

                return BaseResponse<ProductModel>.FailureResponse(
                    new() { ex.Message },
                    "Error occurred while processing request"
                );
            }
        }

        public async Task<BaseResponse<ProductCategory>> AddEditProductCategoryAsync(ProductCategoryRequesModel requestModel)
        {
            try
            {
                var request = _mapper.Map<ProductCategoryRequesModel, ProductCategory>(requestModel);
                if (request == null)
                    return new BaseResponse<ProductCategory>(
                        new List<string> { "Error" }, "Something went wrong");

                if (requestModel.Id.HasValue)
                    request.ModifiedOn = DateTime.Now;
                else
                    request.CreatedOn = DateTime.Now;

                return await _productRepo.AddEditProductCategoryAsync(request);
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(
                    ex,
                    userId: requestModel.UserId,
                    additionalData: JsonSerializer.Serialize(new { message = "Error while adding or updating product" }),
                    request: JsonSerializer.Serialize(requestModel)
                );

                return BaseResponse<ProductCategory>.FailureResponse(
                    new() { ex.Message },
                    "Error occurred while processing request"
                );
            }
        }
        public async Task<BaseResponse<(List<GetProductResponseModel>, int)>> GetProductsReportAsync(int productId, int pageIndex, int pageSize, string? Filter)
        {
            try
            {
                var getProduct = await GetProductsAsync(productId, pageIndex, pageSize, Filter);
                var productCodes = getProduct.Data.Item1.Select(p => p.ProductCode).ToList();
                var getPricing = await _productRepo.GetPricingByProductCodeAsync(productCodes);

                var result = (
                    from p in getProduct.Data.Item1
                    join pr in getPricing
                        on p.ProductCode equals pr.ProductCode into prj
                    from pr in prj.DefaultIfEmpty() // left join
                    select new GetProductResponseModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        ProductCode = p.ProductCode,
                        CategoryId = p.CategoryId,
                        CreatedOn = p.CreatedOn,
                        CreatedBy = p.CreatedBy,
                        ModifiedOn = p.ModifiedOn,
                        ModifiedBy = p.ModifiedBy,
                        //Stock = p.Stock,
                        Category = p.Category,
                        TotalStock=p.Stocks?.Sum(s=>s.Quantity) ?? 0, // safe access
                        TotalPurchasePrice = p.Stocks?.Sum(s => s.TotalPurchasePrice) ?? 0, // safe access
                        PricingModel = _mapper.Map<Pricing, PricingModel>(pr) // safe access
                    }).ToList();

                if (result == null || result.Count == 0)
                    return new BaseResponse<(List<GetProductResponseModel>, int)>(
                        new List<string> { "Product not found" }, "Error fetching product");

                return new BaseResponse<(List<GetProductResponseModel>, int)>((result, getProduct.Data.Item2), string.Empty);
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(ex, userId: null, additionalData: "{ \"message\": error while fetching }");

                return new BaseResponse<(List<GetProductResponseModel>, int)>(
                        new List<string> { ex.Message }, "Error fetching product");
            }

        }
        public async Task<BaseResponse<VendorStockModel>> AddEditVendorStockAsync(AddEditVendorStockRequestModel requestModel)
        {
            try
            {
                var stockResult = await _productRepo.AddEditVendorStockAsync(new VendorStock
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
                return BaseResponse<VendorStockModel>.SuccessResponse(data,"Request executed successfully");
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
                (List<VendorStock>,int) getStocks = await _productRepo.GetStockAsync(productId, pageIndex, pageSize, Filter);
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

