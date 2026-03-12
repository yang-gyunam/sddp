using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces.Snapshots;

/// <summary>
/// Dapper-based service for project snapshot CRUD and data extraction/restoration
/// </summary>
public interface IProjectSnapshotService
{
    /// <summary>
    /// Extract all project data into a JSONB snapshot and persist it
    /// </summary>
    Task<ProjectSnapshotDto> CreateSnapshotAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId createdBy,
        string name,
        string? description,
        string snapshotType = "manual",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restore project data from a snapshot (delete current + insert from snapshot)
    /// Automatically creates a pre_restore safety snapshot before restoring.
    /// </summary>
    Task<ProjectSnapshotDto> RestoreSnapshotAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId snapshotId,
        GlobalUniqueId restoredBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-delete a snapshot
    /// </summary>
    Task DeleteSnapshotAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId snapshotId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all active snapshots for a project (metadata only, no snapshot_data)
    /// </summary>
    Task<IReadOnlyList<ProjectSnapshotDto>> GetSnapshotsAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete all project-scoped data (specs, requirements, conversations, etc.)
    /// Preserves the project record itself, members, and snapshots.
    /// Returns deleted row counts per table.
    /// </summary>
    Task<Dictionary<string, int>> ResetProjectDataAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete TenantWide conversations (project_id IS NULL) for a tenant.
    /// Used during tenant-level reset to clean up global conversations.
    /// Returns number of deleted conversations.
    /// </summary>
    Task<int> DeleteTenantWideConversationsAsync(
        GlobalUniqueId tenantId,
        CancellationToken cancellationToken = default);
}
