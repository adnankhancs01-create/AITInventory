namespace Common.RetailPos2
{
    public class RetailPosStatementDto2
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<RetailPosStatementRowDto2> Rows { get; set; } = new();
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        /// <summary>Balance at the end of the period (the full account balance when no end date is set).</summary>
        public decimal ClosingBalance { get; set; }
    }
}
