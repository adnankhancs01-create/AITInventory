namespace Common.RetailPos2
{
    public class RetailPosProductLookupDto2
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string Display => $"{ProductCode} - {ProductName}";
    }
}
