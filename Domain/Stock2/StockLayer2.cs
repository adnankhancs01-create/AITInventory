namespace Domain.Stock2
{
    /// <summary>The remaining part of one positive ledger row after FIFO consumption.</summary>
    public class StockLayer2
    {
        public int VendorStockId { get; set; }
        public int ProductId { get; set; }
        public DateTime ReceivedOn { get; set; }
        public int OriginalQuantity { get; set; }
        public int RemainingQuantity { get; set; }
        public decimal UnitCost { get; set; }

        /// <summary>True when the row had no purchase price and the product's average cost was used.</summary>
        public bool IsEstimatedCost { get; set; }
    }
}
