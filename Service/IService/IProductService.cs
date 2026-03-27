using Common;
using Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.IService
{
    public interface IProductService
    {
        Task<BaseResponse<List<ProductModel>>> GetProductsAsync(int? productId);
    }
}
