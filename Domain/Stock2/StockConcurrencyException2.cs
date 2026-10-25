namespace Domain.Stock2
{
    /// <summary>
    /// Raised by the data layer when a row was changed by someone else after it was loaded (RowVersion mismatch).
    /// It lets the service layer react to a concurrency conflict without knowing anything about EF Core.
    /// </summary>
    public class StockConcurrencyException2 : Exception
    {
        public StockConcurrencyException2()
            : base("Another user changed this record while you were working. Reload and try again.")
        {
        }

        public StockConcurrencyException2(Exception innerException)
            : base("Another user changed this record while you were working. Reload and try again.", innerException)
        {
        }
    }
}
