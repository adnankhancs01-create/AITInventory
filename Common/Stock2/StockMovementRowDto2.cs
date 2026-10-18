namespace Common.Stock2
{
    public class StockMovementRowDto2
    {
        public DateTime MovementDate { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;

        /// <summary>Signed quantity (+ in, - out).</summary>
        public int Quantity { get; set; }

        public decimal? UnitCost { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;

        /// <summary>True when the ledger row was deactivated (transaction reverted / receipt cancelled / edited).</summary>
        public bool IsReversed { get; set; }

        /// <summary>"Ledger" = VendorStock row; "Stock2" = location-only movement.</summary>
        public string Source { get; set; } = string.Empty;
    }
}
