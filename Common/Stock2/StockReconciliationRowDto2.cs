namespace Common.Stock2
{
    public class StockReconciliationRowDto2
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int LedgerOnHand { get; set; }
        public int DefaultLocationQty { get; set; }
        public int OtherLocationsQty { get; set; }
        public int InTransitQty { get; set; }
        public int LayerRemaining { get; set; }

        /// <summary>Empty when the product reconciles cleanly.</summary>
        public string Issue { get; set; } = string.Empty;
    }
}
