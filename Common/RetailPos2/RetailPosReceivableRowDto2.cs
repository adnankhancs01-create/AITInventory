namespace Common.RetailPos2
{
    public class RetailPosReceivableRowDto2
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalAdjustments { get; set; }
        public decimal Balance { get; set; }
        public decimal OverdueAmount { get; set; }
        public DateTime? OldestDueDate { get; set; }
    }
}
