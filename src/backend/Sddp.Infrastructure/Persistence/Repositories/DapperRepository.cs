using System.Data;
using Dapper;
using Sddp.Abstractions.DTOs;

namespace Sddp.Infrastructure.Persistence.Repositories;

/// <summary>
/// Query Side Repository (Dapper)
/// </summary>
public class DapperRepository<T> : IReadRepository<T> where T : class
{
    private readonly IDbConnection _connection;

    public DapperRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <param name="sql">Trusted SQL query string. Must NOT contain user input — use Dapper parameters instead.</param>
    public async Task<T?> QuerySingleAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        ValidateSql(sql);
        return await _connection.QueryFirstOrDefaultAsync<T>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    /// <param name="sql">Trusted SQL query string. Must NOT contain user input — use Dapper parameters instead.</param>
    public async Task<IReadOnlyList<T>> QueryAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        ValidateSql(sql);
        var result = await _connection.QueryAsync<T>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return result.ToList();
    }

    /// <param name="sql">Trusted SQL query string. Must NOT contain user input — use Dapper parameters instead.</param>
    public async Task<PagedResult<T>> QueryPagedAsync(string sql, QueryParameters queryParameters, object? parameters = null, CancellationToken cancellationToken = default)
    {
        ValidateSql(sql);

        // COUNT + SELECT layout (1 roundtrip)
        var batchSql = $"SELECT COUNT(*) FROM ({sql}) AS _c; {sql} LIMIT @PageSize OFFSET @Skip";
        var pagedParameters = new DynamicParameters(parameters);
        pagedParameters.Add("PageSize", queryParameters.PageSize);
        pagedParameters.Add("Skip", queryParameters.Skip);

        using var multi = await _connection.QueryMultipleAsync(
            new CommandDefinition(batchSql, pagedParameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
        var totalCount = await multi.ReadSingleAsync<int>().ConfigureAwait(false);
        var items = (await multi.ReadAsync<T>().ConfigureAwait(false)).ToList();

        return PagedResult<T>.Create(
            items,
            totalCount,
            queryParameters.PageNumber,
            queryParameters.PageSize
        );
    }

    /// <param name="sql">Trusted SQL query string. Must NOT contain user input — use Dapper parameters instead.</param>
    public async Task<int> ExecuteAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        ValidateSql(sql);
        return await _connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static void ValidateSql(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql, nameof(sql));

        if (sql.Contains(';'))
            throw new ArgumentException("SQL must not contain semicolons; parameterize values instead.", nameof(sql));

        if (sql.Contains("--") || sql.Contains("/*"))
            throw new ArgumentException("SQL must not contain comment sequences.", nameof(sql));
    }
}
