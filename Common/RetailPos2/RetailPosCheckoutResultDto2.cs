namespace Common.RetailPos2
{
    public class RetailPosCheckoutResultDto2
    {
        public int SaleId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public bool WasDuplicate { get; set; }
    }
}
