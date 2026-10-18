namespace Common.Stock2
{
    public class ProductLookupDto2
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int OnHand { get; set; }
        public decimal LastUnitCost { get; set; }
        public string DisplayName => $"{ProductCode} - {ProductName}";
    }
}
