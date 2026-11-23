using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Sddp.Api.HealthChecks;

/// <summary>
/// Verifies database connectivity and minimal provisioning readiness.
/// </summary>
public sealed class DatabaseReadinessHealthCheck(IConfiguration configuration) : IHealthCheck
{
    private readonly string? _connectionString = configuration.GetConnectionString("DefaultConnection");

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return HealthCheckResult.Unhealthy("DefaultConnection is not configured.");
        }

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string requiredTablesSql = """
                SELECT COUNT(*)
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_name IN ('group_codes', 'codes', 'users', 'projects');
                """;

            await using var requiredTablesCommand = new NpgsqlCommand(requiredTablesSql, connection);
            var requiredTableCount = (long)(await requiredTablesCommand.ExecuteScalarAsync(cancellationToken) ?? 0L);
            if (requiredTableCount < 4)
            {
                return HealthCheckResult.Unhealthy("Database schema is not fully provisioned.");
            }

            await using var baseDataCommand = new NpgsqlCommand("SELECT COUNT(*) FROM group_codes;", connection);
            var groupCodeCount = (long)(await baseDataCommand.ExecuteScalarAsync(cancellationToken) ?? 0L);
            if (groupCodeCount == 0)
            {
                return HealthCheckResult.Unhealthy("Database base data is missing.");
            }

            const string storageReadinessSql = """
                SELECT name, to_regclass(name) IS NOT NULL AS present,
                       bool_and(coalesce(has_table_privilege(to_regclass(name), privilege), false)) AS usable
                FROM (VALUES
                    (1, 'public.statistics_consents', ARRAY['SELECT','INSERT','UPDATE','DELETE']),
                    (2, 'public.project_search_file_heads', ARRAY['SELECT','INSERT','UPDATE']),
                    (3, 'public.project_search_files', ARRAY['SELECT','INSERT','UPDATE','DELETE'])
                ) AS storage(priority, name, privileges)
                CROSS JOIN LATERAL unnest(privileges) AS required(privilege)
                GROUP BY priority, name ORDER BY priority;
                """;

            await using var storageCommand = new NpgsqlCommand(storageReadinessSql, connection);
            await using var storage = await storageCommand.ExecuteReaderAsync(cancellationToken);
            while (await storage.ReadAsync(cancellationToken))
            {
                var consent = storage.GetString(0) == "public.statistics_consents";
                if (!storage.GetBoolean(1))
                    return HealthCheckResult.Unhealthy(consent
                        ? "Weekly statistics consent table is missing. Apply migration V025."
                        : "Project search storage is missing. Apply migration V027.");
                if (!storage.GetBoolean(2))
                    return HealthCheckResult.Unhealthy(consent
                        ? "The API database role lacks required privileges on the weekly statistics consent table."
                        : "The API database role lacks required privileges on project search storage.");
            }

            return HealthCheckResult.Healthy("Database schema and base data are ready.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to validate database readiness.", ex);
        }
    }
}
