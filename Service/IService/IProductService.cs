using Common;
using Common.Models;
using Common.Models.RequestModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.IService
{
    public interface IProductService
    {
        Task<BaseResponse<List<ProductModel>>> GetProductsAsync(int? productId);
        Task<BaseResponse<ProductModel>> AddEditProductAsync(ProductRequesModel product);
        Task<BaseResponse<ProductCategory>> AddEditProductCategoryAsync(ProductCategoryRequesModel requestModel);
    }
}
