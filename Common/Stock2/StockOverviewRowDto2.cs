namespace Common.Stock2
{
    public class StockOverviewRowDto2
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>Total on hand from the VendorStock ledger (all locations plus in transit).</summary>
        public int OnHand { get; set; }

        /// <summary>Quantity at the location chosen in the filter; equals OnHand when no location is chosen.</summary>
        public int QuantityAtLocation { get; set; }

        public int InTransit { get; set; }
        public int ReorderLevel { get; set; }
        public int ReorderQuantity { get; set; }
        public decimal AverageUnitCost { get; set; }
        public decimal StockValue { get; set; }
        public decimal? SellPrice { get; set; }
        public DateTime? LastMovementOn { get; set; }
        public string StockStatus { get; set; } = string.Empty;
    }
}
