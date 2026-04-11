using Common;
using Common.Models;
using Common.Models.RequestModel;
using Common.Models.ResponseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.IService
{
    public interface IProductService
    {
        Task<BaseResponse<(List<ProductModel>, int)>> GetProductsAsync(int productId, int pageIndex, int pageSize, string? Filter);
        Task<BaseResponse<(List<ProductCategoryModel>, int)>> GetProductCategoriesAsync(int id, int pageIndex, int pageSize, string? Filter);
        Task<BaseResponse<ProductModel>> AddEditProductAsync(ProductRequesModel product);
        Task<BaseResponse<ProductCategoryModel>> AddEditProductCategoryAsync
            (ProductCategoryRequesModel requestModel);
        Task<BaseResponse<(List<GetProductResponseModel>, int)>> GetProductsReportAsync(int productId, int pageIndex, int pageSize, string? Filter);
    }
}
