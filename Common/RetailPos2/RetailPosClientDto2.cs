namespace Common.RetailPos2
{
    public class RetailPosClientDto2
    {
        /// <summary>0 for a new client.</summary>
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        /// <summary>Amount the client already owed when the account was opened (positive = client owes us). Owner only.</summary>
        public decimal OpeningBalance { get; set; }
        /// <summary>Maximum outstanding balance. Empty = no limit. 0 = no credit at all. Owner only.</summary>
        public decimal? CreditLimit { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; }
    }
}
