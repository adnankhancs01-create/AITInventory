using Common;
using Common.Models;
using Common.Models.RequestModel;
using Common.Models.ResponseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.IService
{
    public interface IStockService
    {
        Task<BaseResponse<VendorStockModel>> AddEditVendorStockAsync(AddEditVendorStockRequestModel requestModel);
        Task<BaseResponse<(List<GetStockResponseModel>, int)>> GetStocksReportAsync(int? productId, int pageIndex, int pageSize, string? Filter);
        Task<BaseResponse<(List<StockFifoDto> Data, int TotalCount)>> GetFifoStocksReportAsync(
    int? productId,
    DateTime? fromDate,
    DateTime? toDate,
    int pageIndex,
    int pageSize);
    }
}
