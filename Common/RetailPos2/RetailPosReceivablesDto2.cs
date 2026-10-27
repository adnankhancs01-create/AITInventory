namespace Common.RetailPos2
{
    public class RetailPosReceivablesDto2
    {
        public List<RetailPosReceivableRowDto2> Rows { get; set; } = new();
        public decimal TotalReceivable { get; set; }
        public decimal TotalOverdue { get; set; }
    }
}
