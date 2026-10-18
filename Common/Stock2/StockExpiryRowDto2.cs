namespace Common.Stock2
{
    public class StockExpiryRowDto2
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysToExpiry { get; set; }
        public int RemainingQuantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal ValueAtRisk { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
