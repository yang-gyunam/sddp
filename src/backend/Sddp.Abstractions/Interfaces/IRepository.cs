using System.Linq.Expressions;
using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// Command-side repository interface backed by EF Core.
/// </summary>
public interface IRepository<T> where T : EntityBase
{
    Task<T?> GetByIdAsync(GlobalUniqueId id, CancellationToken cancellationToken = default);
    Task<T?> GetByIdIncludingInactiveAsync(GlobalUniqueId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllIncludingInactiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindIncludingInactiveAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<T> Items, int TotalCount)> FindPagedAsync(
        Expression<Func<T, bool>> predicate,
        int page,
        int pageSize,
        Expression<Func<T, object>> orderBy,
        bool descending = false,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<T> Items, int TotalCount)> FindPagedAsync(
        Expression<Func<T, bool>> predicate,
        int page,
        int pageSize,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderByFactory,
        CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(GlobalUniqueId id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}
