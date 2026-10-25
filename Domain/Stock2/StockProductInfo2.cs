namespace Domain.Stock2
{
    /// <summary>Slim read model of a product (with its category name) used by the stock reports.</summary>
    public class StockProductInfo2
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}
