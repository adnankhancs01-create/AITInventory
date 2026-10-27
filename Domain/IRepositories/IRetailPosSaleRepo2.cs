using Common.RetailPos2;
using Domain.Entities;
using Domain.RetailPos2;

namespace Domain.IRepositories
{
    public interface IRetailPosSaleRepo2
    {
        Task<int> CountSalesWithPrefixAsync(string numberPrefix);
        void AddSale(RetailPosSale2 sale);

        /// <summary>The sale created earlier with the same idempotency key, if any.</summary>
        Task<RetailPosSale2?> FindSaleByKeyAsync(Guid idempotencyKey);
        /// <summary>Tracked sale header (changing it makes the concurrency check fire for parallel payments).</summary>
        Task<RetailPosSale2?> GetSaleAsync(int saleId);
        Task<RetailPosSaleBalance2?> GetSaleBalanceAsync(int saleId);

        /// <summary>Invoice rows with paid and adjustment amounts filled in; status and outstanding are finished by the service.</summary>
        Task<List<RetailPosSaleRowDto2>> GetSaleRowsAsync(RetailPosSaleFilterDto2 filter);
        Task<RetailPosSaleRowDto2?> GetSaleRowAsync(int saleId);
        Task<List<RetailPosSaleLineRowDto2>> GetSaleLinesAsync(int saleId);

        Task<List<RetailPosProductLookupDto2>> GetProductLookupAsync();
        Task<Dictionary<int, RetailPosProductInfo2>> GetActiveProductsAsync(ICollection<int> productIds);
    }
}
