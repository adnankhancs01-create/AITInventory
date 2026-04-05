using AutoMapper;
using Common;
using Common.Models;
using Common.Models.RequestModel;
using Domain.Entities;
using Domain.IRepositories;
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
                    (requestModel.Quantity.HasValue ||requestModel.UnitPrice.HasValue ||requestModel.SellPrice.HasValue ))
                {
                    await _productRepo.AddEditVendorStockAsync(new VendorStock { 
                        ProductId=result.Data.Id,
                        Quantity=requestModel.Quantity,
                        SellPrice=requestModel.SellPrice,
                        UnitPrice=requestModel.UnitPrice
                        //,
                        //VendorId=requestModel.ve
                    });
                }
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
    }
}

