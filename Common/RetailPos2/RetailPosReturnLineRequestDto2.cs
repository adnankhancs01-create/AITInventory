namespace Common.RetailPos2
{
    public class RetailPosReturnLineRequestDto2
    {
        public int SaleLineId { get; set; }
        public int Quantity { get; set; }
        /// <summary>ReturnToStock, or a write-off reason (Damaged, Expired, Lost / Stolen, Internal use, Sample / Promotion, Other).</summary>
        public string StockAction { get; set; } = RetailPosConstants2.StockReturnToStock;
    }
}
