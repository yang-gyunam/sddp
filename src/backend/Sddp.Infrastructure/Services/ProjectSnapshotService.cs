using System.Data;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces.Snapshots;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Infrastructure.Services;

/// <summary>
/// Dapper implementation of IProjectSnapshotService.
/// Extracts, stores, deletes, and restores all project-scoped data via raw SQL.
/// </summary>
public partial class ProjectSnapshotService : IProjectSnapshotService
{
    private readonly IDbConnection _connection;
    private readonly ILogger<ProjectSnapshotService> _logger;

    public ProjectSnapshotService(IDbConnection connection, ILogger<ProjectSnapshotService> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    // ========================================================================
    // Public API
    // ========================================================================

    public async Task<ProjectSnapshotDto> CreateSnapshotAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId createdBy,
        string name,
        string? description,
        string snapshotType = "manual",
        CancellationToken cancellationToken = default)
    {
        EnsureOpen();

        var data = await (ExtractProjectDataAsync(tenantId, projectId)).ConfigureAwait(false);
        var jsonData = JsonSerializer.Serialize(data);
        var dataSize = Encoding.UTF8.GetByteCount(jsonData);
        var tableCounts = data.Tables.ToDictionary(kv => kv.Key, kv => kv.Value.Count);
        var tableCountsJson = JsonSerializer.Serialize(tableCounts);

        var snapshotId = Guid.CreateVersion7();

        const string sql = @"
            INSERT INTO project_snapshots
                (id, tenant_id, project_id, name, description, snapshot_type, status,
                 snapshot_data, table_counts, data_size_bytes, created_by, created_at, is_active)
            VALUES
                (@Id, @TenantId, @ProjectId, @Name, @Description, @SnapshotType, 'completed',
                 @SnapshotData::jsonb, @TableCounts::jsonb, @DataSizeBytes, @CreatedBy, NOW(), TRUE)";

        await (_connection.ExecuteAsync(sql, new
        {
            Id = snapshotId,
            TenantId = (Guid)tenantId,
            ProjectId = (Guid)projectId,
            Name = name,
            Description = description,
            SnapshotType = snapshotType,
            SnapshotData = jsonData,
            TableCounts = tableCountsJson,
            DataSizeBytes = dataSize,
            CreatedBy = (Guid)createdBy,
        })).ConfigureAwait(false);

        _logger.LogInformation(
            "Created {SnapshotType} snapshot {SnapshotId} for project {ProjectId} ({DataSize} bytes, {TableCount} tables)",
            snapshotType, snapshotId, (Guid)projectId, dataSize, tableCounts.Count);

        return await (GetSnapshotByIdAsync(snapshotId)).ConfigureAwait(false);
    }

    public async Task<ProjectSnapshotDto> RestoreSnapshotAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId snapshotId,
        GlobalUniqueId restoredBy,
        CancellationToken cancellationToken = default)
    {
        EnsureOpen();

        // 1. Load the snapshot data
        const string loadSql = @"
            SELECT snapshot_data FROM project_snapshots
            WHERE id = @Id AND tenant_id = @TenantId AND project_id = @ProjectId AND is_active = TRUE";
        var snapshotJson = await (_connection.QuerySingleOrDefaultAsync<string>(loadSql, new
        {
            Id = (Guid)snapshotId,
            TenantId = (Guid)tenantId,
            ProjectId = (Guid)projectId,
        })).ConfigureAwait(false);

        if (snapshotJson is null)
        {
            throw new InvalidOperationException($"Snapshot {snapshotId} not found");
        }

        var snapshotData = JsonSerializer.Deserialize<ProjectSnapshotData>(snapshotJson)
            ?? throw new InvalidOperationException("Failed to deserialize snapshot data");

        // 2. Create a safety pre_restore snapshot
        await (CreateSnapshotAsync(tenantId, projectId, restoredBy,
            $"Pre-restore backup ({DateTime.UtcNow:yyyy-MM-dd HH:mm})",
            $"Automatic backup before restoring snapshot {snapshotId}",
            "pre_restore", cancellationToken)).ConfigureAwait(false);

        // 3. Delete current project data, then insert from snapshot — all in one transaction
        using var transaction = _connection.BeginTransaction();
        try
        {
            await (DeleteProjectDataAsync(tenantId, projectId, transaction)).ConfigureAwait(false);
            await (RestoreProjectDataAsync(snapshotData, transaction)).ConfigureAwait(false);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        _logger.LogInformation(
            "Restored snapshot {SnapshotId} for project {ProjectId}",
            (Guid)snapshotId, (Guid)projectId);

        return await (GetSnapshotByIdAsync((Guid)snapshotId)).ConfigureAwait(false);
    }

    public async Task DeleteSnapshotAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId snapshotId,
        CancellationToken cancellationToken = default)
    {
        EnsureOpen();

        const string sql = @"
            UPDATE project_snapshots SET is_active = FALSE
            WHERE id = @Id AND tenant_id = @TenantId AND project_id = @ProjectId";

        await (_connection.ExecuteAsync(sql, new
        {
            Id = (Guid)snapshotId,
            TenantId = (Guid)tenantId,
            ProjectId = (Guid)projectId,
        })).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ProjectSnapshotDto>> GetSnapshotsAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        CancellationToken cancellationToken = default)
    {
        EnsureOpen();

        const string sql = @"
            SELECT
                s.id, s.project_id, s.name, s.description,
                s.snapshot_type, s.status, s.table_counts,
                s.data_size_bytes, s.created_by,
                COALESCE(p.display_name, u.username, 'Unknown') AS created_by_name,
                s.created_at
            FROM project_snapshots s
            LEFT JOIN users u ON u.id = s.created_by
            LEFT JOIN persons p ON p.id = u.person_id
            WHERE s.tenant_id = @TenantId
              AND s.project_id = @ProjectId
              AND s.is_active = TRUE
            ORDER BY s.created_at DESC";

        var rows = await (_connection.QueryAsync(sql, new
        {
            TenantId = (Guid)tenantId,
            ProjectId = (Guid)projectId,
        })).ConfigureAwait(false);

        return rows.Select(r => MapToDto((object)r)).ToList();
    }

    private void EnsureOpen()
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
    }
}
