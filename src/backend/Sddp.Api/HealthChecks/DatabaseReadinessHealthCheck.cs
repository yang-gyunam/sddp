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

            const string consentReadinessSql = """
                SELECT consent.table_oid IS NOT NULL,
                       COALESCE(has_table_privilege(consent.table_oid, 'SELECT')
                            AND has_table_privilege(consent.table_oid, 'INSERT')
                            AND has_table_privilege(consent.table_oid, 'UPDATE')
                            AND has_table_privilege(consent.table_oid, 'DELETE'), FALSE)
                FROM (SELECT to_regclass('public.statistics_consents')::oid AS table_oid) AS consent;
                """;

            await using var consentCommand = new NpgsqlCommand(consentReadinessSql, connection);
            await using var consent = await consentCommand.ExecuteReaderAsync(cancellationToken);
            if (!await consent.ReadAsync(cancellationToken) || !consent.GetBoolean(0))
            {
                return HealthCheckResult.Unhealthy("Weekly statistics consent table is missing. Apply migration V025.");
            }

            if (!consent.GetBoolean(1))
            {
                return HealthCheckResult.Unhealthy("The API database role lacks required privileges on the weekly statistics consent table.");
            }

            return HealthCheckResult.Healthy("Database schema and base data are ready.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to validate database readiness.", ex);
        }
    }
}
