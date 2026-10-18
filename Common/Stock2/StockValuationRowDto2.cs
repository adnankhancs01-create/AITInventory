namespace Common.Stock2
{
    public class StockValuationRowDto2
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int OnHand { get; set; }
        public decimal AverageUnitCost { get; set; }
        public decimal StockValue { get; set; }

        /// <summary>True when some remaining layers have no purchase price and a fallback cost was used.</summary>
        public bool HasEstimatedCost { get; set; }
    }
}
