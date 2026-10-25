namespace Domain.Stock2
{
    /// <summary>Slim read model of a batch that has an expiry date.</summary>
    public class StockBatchInfo2
    {
        public int ProductId { get; set; }
        public int VendorStockId { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
