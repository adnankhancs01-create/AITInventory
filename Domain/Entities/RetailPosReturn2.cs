namespace Domain.Entities
{
    /// <summary>A return of goods from one invoice (table RetailPos2_Returns). Never edited: it is the audit record of the return.</summary>
    public class RetailPosReturn2
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;

        public int SaleId { get; set; }
        public RetailPosSale2? Sale { get; set; }

        /// <summary>Null = the invoice was a walk-in sale.</summary>
        public int? ClientId { get; set; }
        public RetailPosClient2? Client { get; set; }

        public DateTime ReturnDate { get; set; } = DateTime.Now;

        /// <summary>Value of the returned goods, taken off the invoice (ledger: sale return).</summary>
        public decimal CreditAmount { get; set; }
        /// <summary>Money paid back to the client, when the invoice was already paid beyond its new value (ledger: refund).</summary>
        public decimal RefundAmount { get; set; }
        public string? RefundMethod { get; set; }

        public string Reason { get; set; } = string.Empty;
        public Guid IdempotencyKey { get; set; }

        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public ICollection<RetailPosReturnLine2> Lines { get; set; } = new List<RetailPosReturnLine2>();
    }
}
