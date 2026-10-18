namespace Common.Stock2
{
    public class StockAgingRowDto2
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Qty0To30 { get; set; }
        public int Qty31To60 { get; set; }
        public int Qty61To90 { get; set; }
        public int QtyOver90 { get; set; }
        public int TotalQty { get; set; }
        public decimal ValueOver90 { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime? OldestLayerDate { get; set; }
    }
}
