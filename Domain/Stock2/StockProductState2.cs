namespace Domain.Stock2
{
    /// <summary>On-hand quantity and FIFO cost layers for one product, derived from the Stock ledger.</summary>
    public class StockProductState2
    {
        public int ProductId { get; set; }

        /// <summary>Sum of all active ledger quantities. Can be negative when more was sold than received.</summary>
        public int OnHand { get; set; }

        /// <summary>Layers with remaining quantity, oldest first.</summary>
        public List<StockLayer2> Layers { get; set; } = new();

        public decimal StockValue { get; set; }
        public decimal AverageUnitCost { get; set; }
        public decimal LastUnitCost { get; set; }
        public bool HasEstimatedCost { get; set; }
    }
}
