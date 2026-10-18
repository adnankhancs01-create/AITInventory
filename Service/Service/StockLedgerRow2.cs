namespace Service.Service
{
    /// <summary>Slim projection of an active VendorStock row used by the layer calculator.</summary>
    public class StockLedgerRow2
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        /// <summary>Signed quantity. Positive = stock in (purchase, receipt, return, adjustment). Negative = stock out (sale, issue, adjustment).</summary>
        public int Quantity { get; set; }

        /// <summary>Total purchase price of the row (not per unit). Null/0 when the cost is unknown.</summary>
        public decimal? TotalPurchasePrice { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}
