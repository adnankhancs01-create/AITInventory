namespace Domain.IRepositories
{
    /// <summary>
    /// Creates a new, independent unit of work per operation. The application keeps one long-lived database
    /// context, so every stock operation gets its own short-lived context from this factory.
    /// </summary>
    public interface IStockUnitOfWorkFactory2
    {
        IStockUnitOfWork2 Create();
    }
}
