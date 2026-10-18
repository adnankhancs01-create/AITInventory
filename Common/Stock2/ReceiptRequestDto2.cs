namespace Common.Stock2
{
    public class ReceiptRequestDto2 : StockDocumentRequestDto2
    {
        public int? VendorId { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierInvoiceNo { get; set; }
        public int? PurchaseOrderId { get; set; }
        public int LocationId { get; set; }
    }
}
