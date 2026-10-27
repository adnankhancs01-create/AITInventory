namespace Domain.Entities
{
    /// <summary>A Retail POS invoice (table RetailPos2_Sales). Paid and outstanding amounts are calculated from the payments, never stored.</summary>
    public class RetailPosSale2
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; } = DateTime.Now;

        /// <summary>Null = walk-in sale (always paid in full).</summary>
        public int? ClientId { get; set; }
        public RetailPosClient2? Client { get; set; }

        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        /// <summary>How the sale was settled at checkout: Paid, PartiallyPaid or Credit. Fixed after checkout.</summary>
        public string PaymentType { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public bool CreditLimitOverridden { get; set; }
        public string? Remarks { get; set; }

        /// <summary>Blocks the same checkout form from creating two sales.</summary>
        public Guid IdempotencyKey { get; set; }

        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime? ModifiedOn { get; set; }
        public byte[]? RowVersion { get; set; }

        public ICollection<RetailPosSaleLine2> Lines { get; set; } = new List<RetailPosSaleLine2>();
        public ICollection<RetailPosPayment2> Payments { get; set; } = new List<RetailPosPayment2>();
    }
}
