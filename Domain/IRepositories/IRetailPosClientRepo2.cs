using Common.RetailPos2;
using Domain.Entities;

namespace Domain.IRepositories
{
    public interface IRetailPosClientRepo2
    {
        /// <summary>Tracked client (for changes).</summary>
        Task<RetailPosClient2?> GetClientAsync(int clientId);
        Task<RetailPosClient2?> FindClientAsync(int clientId);
        Task<bool> ClientExistsAsync(string name, string? phone, int excludeClientId);
        void AddClient(RetailPosClient2 client);

        Task<List<RetailPosClientRowDto2>> GetClientRowsAsync(RetailPosClientFilterDto2 filter);
        Task<RetailPosClientRowDto2?> GetClientRowAsync(int clientId);

        /// <summary>Opening balance + sales + adjustments - payments that are not reversed.</summary>
        Task<decimal> GetBalanceAsync(int clientId);
    }
}
