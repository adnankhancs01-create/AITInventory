namespace Common.RetailPos2
{
    public class RetailPosReturnRowDto2
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public int SaleId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Items { get; set; } = string.Empty;
        public decimal CreditAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public string RefundMethod { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }
}
