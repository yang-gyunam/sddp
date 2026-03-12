using Sddp.Abstractions.Base;

namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// Unit of Work
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// entity Repository
    /// </summary>
    IRepository<T> Repository<T>() where T : EntityBase;

    /// <summary>
    /// change
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///
    /// </summary>
    Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// () unit.
    /// Npgsql ExecutionStrategy user,
    /// .
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
