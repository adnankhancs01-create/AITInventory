namespace Common.Stock2
{
    public class PurchaseOrderRequestDto2 : StockDocumentRequestDto2
    {
        public int? VendorId { get; set; }
        public string? SupplierName { get; set; }
        public DateTime? ExpectedDate { get; set; }
    }
}
