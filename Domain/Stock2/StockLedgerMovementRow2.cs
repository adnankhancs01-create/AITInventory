namespace Domain.Stock2
{
    /// <summary>One row of the Stock ledger together with the number/type of the sale or purchase transaction it belongs to (if any).</summary>
    public class StockLedgerMovementRow2
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal? TotalPurchasePrice { get; set; }
        public long? StockNumber { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool? IsActive { get; set; }

        /// <summary>TransactionMst.TransactionType of the linked transaction ("Sell", "Purchase", "Return"), or null.</summary>
        public string? TransactionType { get; set; }

        /// <summary>TransactionMst.TransactionNumber of the linked transaction, or null.</summary>
        public long? TransactionNumber { get; set; }
    }
}
