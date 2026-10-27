namespace Common.RetailPos2
{
    public class RetailPosPaymentRowDto2
    {
        public int Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public int? ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public int? SaleId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public bool IsReversed { get; set; }
        public DateTime? ReversedOn { get; set; }
        public string ReversalReason { get; set; } = string.Empty;
        public string ReceivedBy { get; set; } = string.Empty;
    }
}
