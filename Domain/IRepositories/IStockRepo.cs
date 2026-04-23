using Common;
using Common.Models.ResponseModel;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.IRepositories
{
    public interface IStockRepo
    {
        Task<BaseResponse<VendorStock>> AddEditVendorStockAsync(VendorStock vendorStock);
        Task<(List<VendorStock>, int)> GetStockAsync(int? productId, int pageIndex, int pageSize, string? filter);
        Task<BaseResponse<bool>> DeductStockAsync(int productId, int quantity);
        Task<(List<StockFifoDto> Data, int TotalCount)> GetFifoStockReport(
    int? productId,
    DateTime? fromDate,
    DateTime? toDate,
    int pageIndex,
    int pageSize);
    }
}
