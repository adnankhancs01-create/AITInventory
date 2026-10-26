namespace Domain.Entities
{
    /// <summary>The single business profile (one row). Users belong to it through UserMst.BusinessId.</summary>
    public class Business2
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? TaxNumber { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}
