using Common;
using Common.RetailPos2;

namespace Service.IService
{
    public interface IRetailPosClientService2
    {
        /// <summary>Clients with their total sales, payments and the amount owed.</summary>
        Task<BaseResponse<List<RetailPosClientRowDto2>>> GetClientsAsync(RetailPosClientFilterDto2 filter);
        Task<BaseResponse<RetailPosClientDto2>> GetClientAsync(int clientId);
        /// <summary>Creates or updates a client. Opening balance and credit limit are owner-only.</summary>
        Task<BaseResponse<int>> SaveClientAsync(RetailPosClientDto2 dto);
    }
}
