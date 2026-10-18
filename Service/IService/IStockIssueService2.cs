using Common;
using Common.Stock2;

namespace Service.IService
{
    public interface IStockIssueService2
    {
        Task<BaseResponse<int>> PostIssueAsync(IssueRequestDto2 request);
        Task<BaseResponse<PagedResult2<DocumentRowDto2>>> GetIssuesAsync(StockQueryFilter2 filter);
        Task<BaseResponse<List<DocumentLineDto2>>> GetIssueLinesAsync(int issueId);
    }
}
