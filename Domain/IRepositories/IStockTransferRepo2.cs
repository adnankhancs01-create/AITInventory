using Common.Stock2;
using Domain.Entities;

namespace Domain.IRepositories
{
    /// <summary>Data access for transfers between locations.</summary>
    public interface IStockTransferRepo2
    {
        Task<int> CountTransfersWithPrefixAsync(string numberPrefix);
        void AddTransfer(StockTransfer2 transfer);

        /// <summary>Tracked transfer including its lines, its source location and its destination location. Null when not found.</summary>
        Task<StockTransfer2?> GetTransferWithLinesAsync(int transferId);

        Task<List<DocumentRowDto2>> GetTransferRowsAsync(StockQueryFilter2 filter);
        Task<List<DocumentLineDto2>> GetTransferLinesAsync(int transferId);
        Task<int> CountInTransitTransfersAsync();
    }
}
