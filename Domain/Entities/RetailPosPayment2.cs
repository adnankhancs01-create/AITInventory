namespace Domain.Entities
{
    /// <summary>
    /// A payment received from a client (table RetailPos2_Payments). Rows are never edited or deleted.
    /// A mistake is corrected by reversing the payment (owner only), which keeps the audit trail.
    /// </summary>
    public class RetailPosPayment2
    {
        public int Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public Guid IdempotencyKey { get; set; }

        public int? ClientId { get; set; }
        public RetailPosClient2? Client { get; set; }

        /// <summary>The invoice being paid. Null = payment against the client's account balance.</summary>
        public int? SaleId { get; set; }
        public RetailPosSale2? Sale { get; set; }

        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        /// <summary>Cash, Card, BankTransfer... (not the same as the sale's payment type).</summary>
        public string Method { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string? Remarks { get; set; }

        public bool IsReversed { get; set; }
        public DateTime? ReversedOn { get; set; }
        public int? ReversedBy { get; set; }
        public string? ReversalReason { get; set; }

        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}
