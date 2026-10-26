namespace Domain.IRepositories
{
    /// <summary>One database context plus one optional transaction for a user-management operation. Dispose to finish; an uncommitted transaction is rolled back.</summary>
    public interface IUserUnitOfWork2 : IAsyncDisposable
    {
        IUserAccountRepo2 Accounts { get; }
        IUserRoleRepo2 Roles { get; }

        Task BeginTransactionAsync();
        Task SaveChangesAsync();
        Task CommitAsync();
    }
}
