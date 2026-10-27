namespace Common.RetailPos2
{
    public class RetailPosSaleDetailDto2
    {
        public RetailPosSaleRowDto2 Sale { get; set; } = new();
        public List<RetailPosSaleLineRowDto2> Lines { get; set; } = new();
        public List<RetailPosPaymentRowDto2> Payments { get; set; } = new();
    }
}
