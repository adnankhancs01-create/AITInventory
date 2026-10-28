namespace Common.RetailPos2
{
    public class RetailPosReturnRequestDto2
    {
        public int SaleId { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        /// <summary>How a refund is paid back when the client has already paid more than the invoice is now worth.</summary>
        public string RefundMethod { get; set; } = RetailPosConstants2.MethodCash;
        public List<RetailPosReturnLineRequestDto2> Lines { get; set; } = new();
        /// <summary>Created once per return form. Submitting the same key twice never creates a second return.</summary>
        public Guid IdempotencyKey { get; set; }
    }
}
