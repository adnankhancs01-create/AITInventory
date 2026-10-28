namespace Common.RetailPos2
{
    public class RetailPosCheckoutLineDto2
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        /// <summary>Discount on this line in percent (0 to 100). The service turns it into money and adds it to the invoice discount.</summary>
        public decimal DiscountPercent { get; set; }
        public decimal LineTotal => Math.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
    }
}
