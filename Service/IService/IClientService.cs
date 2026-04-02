using Common;
using Common.Models;
using Common.Models.RequestModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.IService
{
    public interface IClientService
    {
        Task<BaseResponse<int>> AddEditClientAsync(AddEditClientDetailRequestModel model);
        Task<BaseResponse<(List<ClientModel>, int)>> GetAllClientsAsync(int id, int pageIndex, int pageSize, string? Filter);
    }
}
