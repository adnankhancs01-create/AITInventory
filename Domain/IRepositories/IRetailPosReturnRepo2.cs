using Common.RetailPos2;
using Domain.Entities;
using Domain.RetailPos2;

namespace Domain.IRepositories
{
    /// <summary>Data access for sale returns.</summary>
    public interface IRetailPosReturnRepo2
    {
        Task<int> CountReturnsWithPrefixAsync(string numberPrefix);
        void AddReturn(RetailPosReturn2 saleReturn);
        Task<RetailPosReturn2?> FindReturnByKeyAsync(Guid idempotencyKey);

        /// <summary>Lines of the invoice (not tracked).</summary>
        Task<List<RetailPosSaleLine2>> GetSaleLineEntitiesAsync(int saleId);

        /// <summary>Quantity already returned per invoice line.</summary>
        Task<Dictionary<int, int>> GetReturnedQuantitiesAsync(int saleId);

        /// <summary>Total credit already given for returns of the invoice.</summary>
        Task<decimal> GetCreditedAmountAsync(int saleId);

        /// <summary>What the original sale took out of stock, per product. Empty for invoices made before stock was linked.</summary>
        Task<Dictionary<int, RetailPosSaleStockLine2>> GetSaleStockLinesAsync(int saleId);

        Task<List<RetailPosReturnRowDto2>> GetReturnRowsAsync(RetailPosReturnFilterDto2 filter);
    }
}
