namespace Common.RetailPos2
{
    public class RetailPosStatementRowDto2
    {
        public DateTime Date { get; set; }
        public string EntryType { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        /// <summary>Increases what the client owes (sale, reversal, increasing adjustment).</summary>
        public decimal Debit { get; set; }
        /// <summary>Decreases what the client owes (payment, credit note).</summary>
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
        public string User { get; set; } = string.Empty;
    }
}
