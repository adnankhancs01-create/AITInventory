namespace Common.RetailPos2
{
    public class RetailPosReceiptDto2
    {
        public int PaymentId { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        /// <summary>Outstanding on the paid invoice after this payment (0 when the payment was against the account).</summary>
        public decimal InvoiceBalanceAfter { get; set; }
        /// <summary>Total the client still owes after this payment.</summary>
        public decimal ClientBalanceAfter { get; set; }
        public string ReceivedBy { get; set; } = string.Empty;
        public bool WasDuplicate { get; set; }
    }
}
