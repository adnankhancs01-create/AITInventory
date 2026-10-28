namespace Common.RetailPos2
{
    public class RetailPosReturnPrepDto2
    {
        public RetailPosSaleRowDto2 Sale { get; set; } = new();
        public List<RetailPosReturnableLineDto2> Lines { get; set; } = new();
        /// <summary>False for invoices made before the POS was linked to stock: returning those goods cannot change stock.</summary>
        public bool StockTracked { get; set; }
    }
}
