namespace Common.RetailPos2
{
    public class RetailPosReceivePaymentRequestDto2
    {
        public int ClientId { get; set; }
        /// <summary>The invoice being paid. Null = payment against the agreed account balance.</summary>
        public int? SaleId { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = RetailPosConstants2.MethodCash;
        public string? Reference { get; set; }
        public string? Remarks { get; set; }
        public DateTime? PaymentDate { get; set; }
        /// <summary>Created once per payment form. Submitting the same key twice never records the payment twice.</summary>
        public Guid IdempotencyKey { get; set; }
    }
}
