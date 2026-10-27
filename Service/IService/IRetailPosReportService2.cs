using Common;
using Common.RetailPos2;

namespace Service.IService
{
    public interface IRetailPosReportService2
    {
        /// <summary>Client account statement in date order with a running balance.</summary>
        Task<BaseResponse<RetailPosStatementDto2>> GetStatementAsync(int clientId, DateTime? fromDate, DateTime? toDate);

        /// <summary>Total receivables per client, including overdue amounts when invoices have due dates.</summary>
        Task<BaseResponse<RetailPosReceivablesDto2>> GetReceivablesAsync(DateTime? asOfDate);
    }
}
