namespace Common.Stock2
{
    /// <summary>A line on any Stock2 document request. Only the fields relevant to the document are read.</summary>
    public class DocumentLineRequestDto2
    {
        public int ProductId { get; set; }

        /// <summary>Ordered, received, issued or transferred quantity.</summary>
        public int Quantity { get; set; }

        /// <summary>Adjustment lines only: the physically counted quantity.</summary>
        public int? CountedQuantity { get; set; }

        public decimal UnitCost { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }

        /// <summary>Receipt lines only: the purchase order line this receipt line fulfils.</summary>
        public int? PurchaseOrderLineId { get; set; }

        public string? Remarks { get; set; }
    }
}
