using Common.RetailPos2;
using Domain.Entities;

namespace Domain.IRepositories
{
    public interface IRetailPosPaymentRepo2
    {
        Task<int> CountPaymentsWithPrefixAsync(string numberPrefix);
        Task<RetailPosPayment2?> FindPaymentByKeyAsync(Guid idempotencyKey);
        void AddPayment(RetailPosPayment2 payment);
        /// <summary>Tracked payment (for the reversal).</summary>
        Task<RetailPosPayment2?> GetPaymentAsync(int paymentId);
        Task<RetailPosPaymentRowDto2?> GetPaymentRowAsync(int paymentId);
        Task<List<RetailPosPaymentRowDto2>> GetPaymentRowsAsync(RetailPosPaymentFilterDto2 filter);

        void AddAdjustment(RetailPosAdjustment2 adjustment);
        Task<List<RetailPosAdjustment2>> GetAdjustmentsAsync(int clientId);

        Task<Dictionary<int, string>> GetUserNamesAsync(ICollection<int> userIds);
    }
}
