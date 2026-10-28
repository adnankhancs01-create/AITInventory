namespace Domain.Entities
{
    /// <summary>One returned product line (table RetailPos2_ReturnLines).</summary>
    public class RetailPosReturnLine2
    {
        public int Id { get; set; }
        public int ReturnId { get; set; }
        public RetailPosReturn2? Return { get; set; }

        public int SaleLineId { get; set; }
        public RetailPosSaleLine2? SaleLine { get; set; }

        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }
        /// <summary>The price on the invoice.</summary>
        public decimal UnitPrice { get; set; }
        /// <summary>The credit given for this line (price after the invoice discount).</summary>
        public decimal LineAmount { get; set; }

        /// <summary>ReturnToStock, or the write-off reason (Damaged, Expired...).</summary>
        public string StockAction { get; set; } = string.Empty;
        /// <summary>The stock location touched by the return. Null = the invoice has no stock record, so stock was not changed.</summary>
        public int? LocationId { get; set; }
    }
}
