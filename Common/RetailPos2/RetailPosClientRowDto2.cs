namespace Common.RetailPos2
{
    public class RetailPosClientRowDto2
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal? CreditLimit { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalAdjustments { get; set; }
        /// <summary>Total amount the client owes: opening + sales + adjustments - payments received.</summary>
        public decimal Balance { get; set; }
    }
}
