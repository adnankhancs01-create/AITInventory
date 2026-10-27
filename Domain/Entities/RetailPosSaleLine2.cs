namespace Domain.Entities
{
    /// <summary>One product line of a Retail POS invoice (table RetailPos2_SaleLines). Code and name are copied so history never changes.</summary>
    public class RetailPosSaleLine2
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public RetailPosSale2? Sale { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
