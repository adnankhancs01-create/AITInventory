namespace Common.RetailPos2
{
    public class RetailPosSaleFilterDto2
    {
        public int? ClientId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Search { get; set; }
        public bool OnlyOutstanding { get; set; }
        public bool OnlyOverdue { get; set; }
        /// <summary>Paid, PartiallyPaid or Credit (current status).</summary>
        public string? PaymentStatus { get; set; }
    }
}
