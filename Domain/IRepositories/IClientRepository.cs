using Common;
using Common.Models;
using Common.Models.RequestModel;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.IRepositories
{
    public interface IClientRepository
    {
        Task<BaseResponse<int>> AddEditClientAsync(AddEditClientDetailRequestModel model);
        Task<(List<VendorClientDetail>, int count)> GetAllClientsAsync(int id, int pageIndex, int pageSize, string? Filter);
    }
}
