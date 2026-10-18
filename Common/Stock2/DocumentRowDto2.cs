namespace Common.Stock2
{
    /// <summary>One row in a document list (purchase orders, receipts, issues, transfers, adjustments).</summary>
    public class DocumentRowDto2
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;

        /// <summary>Supplier for POs and receipts, "From -> To" for transfers, location for issues and adjustments.</summary>
        public string Party { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;
        public int LineCount { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }
}
