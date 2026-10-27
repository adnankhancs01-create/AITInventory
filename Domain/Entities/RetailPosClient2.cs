namespace Domain.Entities
{
    /// <summary>A Retail POS customer account (table RetailPos2_Clients). Separate from the existing Client tables.</summary>
    public class RetailPosClient2
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        /// <summary>Amount owed before this system was used (positive = the client owes us).</summary>
        public decimal OpeningBalance { get; set; }
        /// <summary>Null = no limit. 0 = no credit allowed.</summary>
        public decimal? CreditLimit { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public int CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public byte[]? RowVersion { get; set; }
    }
}
