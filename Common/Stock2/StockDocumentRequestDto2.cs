namespace Common.Stock2
{
    /// <summary>Base request for every Stock2 document.</summary>
    public class StockDocumentRequestDto2
    {
        public DateTime? DocumentDate { get; set; }
        public string? Remarks { get; set; }
        public int? UserId { get; set; }
        public List<DocumentLineRequestDto2> Lines { get; set; } = new();
    }
}
