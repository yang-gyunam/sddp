using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Sddp.Api.HealthChecks;

/// <summary>
/// Reports the current DB schema migration version from the schema_migrations table.
/// </summary>
public sealed class SchemaVersionHealthCheck(IConfiguration configuration) : IHealthCheck
{
    private readonly string? _connectionString = configuration.GetConnectionString("DefaultConnection");

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return HealthCheckResult.Degraded("DefaultConnection is not configured.",
                data: new Dictionary<string, object> { ["schemaVersion"] = 0, ["status"] = "unconfigured" });
        }

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if schema_migrations table exists
            const string tableCheckSql = """
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_name = 'schema_migrations'
                );
                """;

            await using var tableCheckCmd = new NpgsqlCommand(tableCheckSql, connection);
            var tableExists = (bool)(await tableCheckCmd.ExecuteScalarAsync(cancellationToken) ?? false);

            if (!tableExists)
            {
                return HealthCheckResult.Healthy(
                    "Pre-migration baseline (schema_migrations table not found).",
                    new Dictionary<string, object> { ["schemaVersion"] = 0, ["status"] = "baseline" });
            }

            // Get latest migration info
            const string versionSql = """
                SELECT version, description, applied_at
                FROM schema_migrations
                ORDER BY version DESC
                LIMIT 1;
                """;

            await using var versionCmd = new NpgsqlCommand(versionSql, connection);
            await using var reader = await versionCmd.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                var version = reader.GetInt32(0);
                var description = reader.GetString(1);
                var appliedAt = reader.GetDateTime(2);

                return HealthCheckResult.Healthy(
                    $"Schema V{version:D3} ({description})",
                    new Dictionary<string, object>
                    {
                        ["schemaVersion"] = version,
                        ["lastMigration"] = $"V{version:D3}__{description}",
                        ["appliedAt"] = appliedAt.ToString("o"),
                        ["status"] = "current"
                    });
            }

            return HealthCheckResult.Healthy(
                "No migrations applied yet.",
                new Dictionary<string, object> { ["schemaVersion"] = 0, ["status"] = "empty" });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Failed to check schema version.", ex,
                new Dictionary<string, object> { ["schemaVersion"] = -1, ["status"] = "error" });
        }
    }
}
