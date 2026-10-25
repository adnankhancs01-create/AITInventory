using Common.Stock2;
using Domain.Entities;

namespace Domain.IRepositories
{
    /// <summary>Data access for stock adjustments and physical counts.</summary>
    public interface IStockAdjustmentRepo2
    {
        Task<int> CountAdjustmentsWithPrefixAsync(string numberPrefix);
        void AddAdjustment(StockAdjustment2 adjustment);
        Task<List<DocumentRowDto2>> GetAdjustmentRowsAsync(StockQueryFilter2 filter);
        Task<List<DocumentLineDto2>> GetAdjustmentLinesAsync(int adjustmentId);
    }
}
