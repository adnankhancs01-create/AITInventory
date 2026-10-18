namespace Common.Stock2
{
    public class StockSettingDto2
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int OnHand { get; set; }
        public int ReorderLevel { get; set; }
        public int ReorderQuantity { get; set; }
        public int? MaxLevel { get; set; }
        public int? PreferredVendorId { get; set; }
    }
}
