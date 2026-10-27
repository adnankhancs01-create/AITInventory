namespace Domain.RetailPos2
{
    /// <summary>Raised by the data layer when a client or invoice was changed by someone else during the operation.</summary>
    public class RetailPosConcurrencyException2 : Exception
    {
        public RetailPosConcurrencyException2()
            : base("Another user changed this record while you were working. Reload and try again.")
        {
        }

        public RetailPosConcurrencyException2(Exception inner)
            : base("Another user changed this record while you were working. Reload and try again.", inner)
        {
        }
    }
}
