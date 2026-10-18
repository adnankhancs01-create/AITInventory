namespace Common.Stock2
{
    public class TransferRequestDto2 : StockDocumentRequestDto2
    {
        public int FromLocationId { get; set; }
        public int ToLocationId { get; set; }
    }
}
