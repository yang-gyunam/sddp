using System.Data;
using System.Data.Common;
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
    private readonly IReadOnlyList<IProjectSnapshotExtension> _extensions;

    public ProjectSnapshotService(IDbConnection connection, ILogger<ProjectSnapshotService> logger,
        IEnumerable<IProjectSnapshotExtension>? extensions = null)
    {
        _connection = connection;
        _logger = logger;
        _extensions = (extensions ?? []).OrderBy(extension => extension.Key, StringComparer.Ordinal).ToArray();
        if (_extensions.Select(extension => extension.Key).Distinct(StringComparer.Ordinal).Count() != _extensions.Count)
            throw new InvalidOperationException("Duplicate project snapshot extension registration.");
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
        try
        {
            using var transaction = _connection.BeginTransaction(IsolationLevel.RepeatableRead);
            await LockProjectAsync(tenantId, projectId, transaction, cancellationToken).ConfigureAwait(false);
            var snapshotId = await SaveSnapshotAsync(tenantId, projectId, createdBy, name, description,
                snapshotType, transaction, cancellationToken).ConfigureAwait(false);
            transaction.Commit();
            return await GetSnapshotByIdAsync(snapshotId).ConfigureAwait(false);
        }
        catch (DbException error) when (error.SqlState == "40001")
        {
            throw new InvalidOperationException("Project data changed concurrently; retry creating the snapshot.", error);
        }
    }

    private async Task<Guid> SaveSnapshotAsync(GlobalUniqueId tenantId, GlobalUniqueId projectId,
        GlobalUniqueId createdBy, string name, string? description, string snapshotType,
        IDbTransaction transaction, CancellationToken cancellationToken)
    {
        var data = await ExtractProjectDataAsync(tenantId, projectId, transaction).ConfigureAwait(false);
        foreach (var extension in _extensions)
            data.Extensions.Add(extension.Key, await extension.CaptureAsync(_connection, transaction,
                tenantId, projectId, cancellationToken).ConfigureAwait(false));
        foreach (var extension in _extensions)
            await extension.ValidateRestoreAsync(_connection, transaction, tenantId, projectId,
                data.Extensions[extension.Key], data, cancellationToken).ConfigureAwait(false);
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
        }, transaction)).ConfigureAwait(false);

        _logger.LogInformation(
            "Created {SnapshotType} snapshot {SnapshotId} for project {ProjectId} ({DataSize} bytes, {TableCount} tables)",
            snapshotType, snapshotId, (Guid)projectId, dataSize, tableCounts.Count);

        return snapshotId;
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

        ValidateSnapshotScope(snapshotData, tenantId, projectId);
        foreach (var key in snapshotData.Extensions.Keys)
            if (!_extensions.Any(extension => extension.Key == key))
                throw new InvalidOperationException($"Snapshot extension '{key}' is not installed.");
        using (var preflight = _connection.BeginTransaction(IsolationLevel.ReadCommitted))
        {
            await LockProjectAsync(tenantId, projectId, preflight, cancellationToken).ConfigureAwait(false);
            await ValidateSnapshotColumnsAsync(snapshotData, preflight, cancellationToken).ConfigureAwait(false);
            foreach (var extension in _extensions)
                await extension.ValidateRestoreAsync(_connection, preflight, tenantId, projectId,
                    snapshotData.Extensions.GetValueOrDefault(extension.Key), snapshotData, cancellationToken).ConfigureAwait(false);
            preflight.Commit();
        }

        // A separate, consistent safety snapshot remains available even if restore later rolls back.
        // Changes made after this backup are not part of its captured point in time.
        await (CreateSnapshotAsync(tenantId, projectId, restoredBy,
            $"Pre-restore backup ({DateTime.UtcNow:yyyy-MM-dd HH:mm})",
            $"Automatic backup before restoring snapshot {snapshotId}",
            "pre_restore", cancellationToken)).ConfigureAwait(false);

        // 3. Delete current project data, then insert from snapshot — all in one transaction
        using var transaction = _connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            await LockProjectAsync(tenantId, projectId, transaction, cancellationToken).ConfigureAwait(false);
            foreach (var extension in _extensions)
                await extension.ValidateRestoreAsync(_connection, transaction, tenantId, projectId,
                    snapshotData.Extensions.GetValueOrDefault(extension.Key), snapshotData, cancellationToken).ConfigureAwait(false);
            foreach (var extension in _extensions)
                await extension.DeleteAsync(_connection, transaction, tenantId, projectId, false, cancellationToken).ConfigureAwait(false);
            await (DeleteProjectDataAsync(tenantId, projectId, transaction)).ConfigureAwait(false);
            await (RestoreProjectDataAsync(snapshotData, transaction)).ConfigureAwait(false);
            foreach (var extension in _extensions)
                if (snapshotData.Extensions.TryGetValue(extension.Key, out var extensionData))
                    await extension.RestoreAsync(_connection, transaction, tenantId, projectId,
                        extensionData, cancellationToken).ConfigureAwait(false);
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
