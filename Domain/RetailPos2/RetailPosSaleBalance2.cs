namespace Domain.RetailPos2
{
    /// <summary>Calculated money position of one invoice.</summary>
    public class RetailPosSaleBalance2
    {
        public int SaleId { get; set; }
        public int? ClientId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        /// <summary>Sum of payments that are not reversed.</summary>
        public decimal PaidAmount { get; set; }
        /// <summary>Sum of credit notes / adjustments linked to the invoice (negative reduces).</summary>
        public decimal AdjustmentAmount { get; set; }

        public decimal OutstandingAmount => RetailPosRules2.Outstanding(TotalAmount, AdjustmentAmount, PaidAmount);
    }
}
