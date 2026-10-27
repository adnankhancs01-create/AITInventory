namespace Common.RetailPos2
{
    public class RetailPosSaleRowDto2
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public int? ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        /// <summary>Credit notes / adjustments linked to this invoice (negative reduces the amount owed).</summary>
        public decimal AdjustmentAmount { get; set; }
        /// <summary>TotalAmount + AdjustmentAmount - PaidAmount. Never negative.</summary>
        public decimal OutstandingAmount { get; set; }
        /// <summary>How the sale was settled at checkout.</summary>
        public string PaymentType { get; set; } = string.Empty;
        /// <summary>Current status calculated from the payments: Paid, PartiallyPaid or Credit.</summary>
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }
}
