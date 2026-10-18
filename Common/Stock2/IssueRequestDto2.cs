namespace Common.Stock2
{
    public class IssueRequestDto2 : StockDocumentRequestDto2
    {
        public int LocationId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
