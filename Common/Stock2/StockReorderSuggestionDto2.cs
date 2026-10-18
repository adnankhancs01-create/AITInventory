namespace Common.Stock2
{
    public class StockReorderSuggestionDto2
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int OnHand { get; set; }
        public int ReorderLevel { get; set; }
        public int ReorderQuantity { get; set; }
        public int MaxLevel { get; set; }

        /// <summary>Outstanding quantity on open / partially received purchase orders.</summary>
        public int OnOrder { get; set; }

        public int SuggestedQuantity { get; set; }
        public decimal LastUnitCost { get; set; }
        public int? PreferredVendorId { get; set; }
        public string PreferredVendorName { get; set; } = string.Empty;
    }
}
