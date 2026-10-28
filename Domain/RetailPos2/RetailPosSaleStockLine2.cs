namespace Domain.RetailPos2
{
    /// <summary>What the original sale took out of stock for one product: where from and at what cost.</summary>
    public class RetailPosSaleStockLine2
    {
        public int ProductId { get; set; }
        public int LocationId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }
}
