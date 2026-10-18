namespace Common.Stock2
{
    /// <summary>A supplier. Backed by the existing Vendor table.</summary>
    public class StockSupplierDto2
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
