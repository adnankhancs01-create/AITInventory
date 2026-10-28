namespace Common.RetailPos2
{
    public class RetailPosReturnableLineDto2
    {
        public int SaleLineId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int SoldQuantity { get; set; }
        public int ReturnedQuantity { get; set; }
        public int RemainingQuantity => SoldQuantity - ReturnedQuantity;
        public decimal UnitPrice { get; set; }
        /// <summary>What the client really paid per piece after the invoice discount. This is the credit given per piece.</summary>
        public decimal CreditUnitPrice { get; set; }

        // Filled in by the screen
        public int ReturnQuantity { get; set; }
        public string StockAction { get; set; } = RetailPosConstants2.StockReturnToStock;
    }
}
