namespace Common.Stock2
{
    public class StockLocationDto2
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
