namespace Common.Stock2
{
    public class DocumentLineDto2
    {
        public int LineId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        /// <summary>Ordered / received / issued / transferred / counted quantity depending on the document.</summary>
        public int Quantity { get; set; }

        /// <summary>PO: quantity received so far. Adjustment: system quantity.</summary>
        public int ProcessedQuantity { get; set; }

        public decimal UnitCost { get; set; }
        public decimal LineValue => Quantity * UnitCost;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public string Note { get; set; } = string.Empty;
    }
}
