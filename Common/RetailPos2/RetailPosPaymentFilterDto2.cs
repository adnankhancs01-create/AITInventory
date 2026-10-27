namespace Common.RetailPos2
{
    public class RetailPosPaymentFilterDto2
    {
        public int? ClientId { get; set; }
        public int? SaleId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Method { get; set; }
        public bool IncludeReversed { get; set; } = true;
    }
}
