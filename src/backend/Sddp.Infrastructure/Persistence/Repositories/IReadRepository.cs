using Sddp.Abstractions.DTOs;

namespace Sddp.Infrastructure.Persistence.Repositories;

/// <summary>
/// Query Side Repository (Dapper)
/// </summary>
public interface IReadRepository<T> where T : class
{
    Task<T?> QuerySingleAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> QueryAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);
    Task<PagedResult<T>> QueryPagedAsync(string sql, QueryParameters queryParameters, object? parameters = null, CancellationToken cancellationToken = default);
    Task<int> ExecuteAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);
}
