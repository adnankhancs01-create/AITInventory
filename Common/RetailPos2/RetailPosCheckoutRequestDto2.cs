namespace Common.RetailPos2
{
    public class RetailPosCheckoutRequestDto2
    {
        /// <summary>Null = walk-in sale (allowed only when the sale is paid in full).</summary>
        public int? ClientId { get; set; }
        public DateTime? SaleDate { get; set; }
        /// <summary>Paid, PartiallyPaid or Credit.</summary>
        public string PaymentType { get; set; } = RetailPosConstants2.Paid;
        /// <summary>Amount received at checkout. Ignored for Paid (= total) and Credit (= 0).</summary>
        public decimal PaidNow { get; set; }
        public string PaymentMethod { get; set; } = RetailPosConstants2.MethodCash;
        public string? PaymentReference { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? Remarks { get; set; }
        public List<RetailPosCheckoutLineDto2> Lines { get; set; } = new();
        /// <summary>Created once per checkout form. Submitting the same key twice never creates a second sale.</summary>
        public Guid IdempotencyKey { get; set; }
    }
}
