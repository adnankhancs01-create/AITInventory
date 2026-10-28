namespace Common.RetailPos2
{
    public class RetailPosReturnResultDto2
    {
        public int ReturnId { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        /// <summary>Value of the returned goods taken off the invoice.</summary>
        public decimal CreditAmount { get; set; }
        /// <summary>Money to hand back to the client (only when the client had already paid more than the invoice is now worth).</summary>
        public decimal RefundAmount { get; set; }
        public decimal InvoiceOutstandingAfter { get; set; }
        public bool WasDuplicate { get; set; }
    }
}
