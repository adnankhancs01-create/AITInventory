using AutoMapper;
using Common;
using Common.Models;
using Domain.IRepositories;
using Service.IService;
using System;
using System.Collections.Generic;
using System.Text;

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

        public async Task<BaseResponse<List<ProductModel>>> GetProductsAsync(int? productId)
        {
            try
            {
                var getProduct = await _productRepo.GetProductsAsync(productId);
                if (getProduct == null)
                    return new BaseResponse<List<ProductModel>>(
                        new List<string> { "Product not found" }, "Error fetching product");

                var data = _mapper.Map<List<Product>, List<ProductModel>>(getProduct);
                return new BaseResponse<List<ProductModel>>(data, string.Empty);
            }
            catch (Exception ex)
            {
                await _logRepo.LogExceptionAsync(ex, userId: 123, additionalData: "{ \"message\": error while fetching }");

                return new BaseResponse<List<ProductModel>>(
                        new List<string> { ex.Message }, "Error fetching product");
            }
        }
    }
}

