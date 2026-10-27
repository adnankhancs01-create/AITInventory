namespace Common.RetailPos2
{
    public class RetailPosAdjustmentRequestDto2
    {
        public int ClientId { get; set; }
        /// <summary>Optional invoice the adjustment applies to.</summary>
        public int? SaleId { get; set; }
        /// <summary>CreditNote or Adjustment.</summary>
        public string AdjustmentType { get; set; } = RetailPosConstants2.AdjustmentCreditNote;
        /// <summary>Always positive. See IncreasesBalance for the direction.</summary>
        public decimal Amount { get; set; }
        /// <summary>False = the client owes less (credit note, write-off). True = the client owes more. A credit note always decreases.</summary>
        public bool IncreasesBalance { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
