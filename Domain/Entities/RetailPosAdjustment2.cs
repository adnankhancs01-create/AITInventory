namespace Domain.Entities
{
    /// <summary>A credit note or approved balance adjustment (table RetailPos2_Adjustments). Owner only, always with a reason.</summary>
    public class RetailPosAdjustment2
    {
        public int Id { get; set; }
        /// <summary>Null for the refund or credit of a walk-in invoice.</summary>
        public int? ClientId { get; set; }
        public RetailPosClient2? Client { get; set; }
        public int? SaleId { get; set; }
        public RetailPosSale2? Sale { get; set; }

        /// <summary>CreditNote or Adjustment (manual), SaleReturn or Refund (written by the return workflow).</summary>
        public string AdjustmentType { get; set; } = string.Empty;
        /// <summary>Signed: negative = the client owes less, positive = the client owes more.</summary>
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;

        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}
