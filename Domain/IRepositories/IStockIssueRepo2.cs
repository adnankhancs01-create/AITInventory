using Common.Stock2;
using Domain.Entities;

namespace Domain.IRepositories
{
    /// <summary>Data access for stock issues (damage, expiry, loss, internal use...).</summary>
    public interface IStockIssueRepo2
    {
        Task<int> CountIssuesWithPrefixAsync(string numberPrefix);
        void AddIssue(StockIssue2 issue);
        Task<List<DocumentRowDto2>> GetIssueRowsAsync(StockQueryFilter2 filter);
        Task<List<DocumentLineDto2>> GetIssueLinesAsync(int issueId);
    }
}
