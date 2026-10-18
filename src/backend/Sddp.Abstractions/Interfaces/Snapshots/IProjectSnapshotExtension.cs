using System.Data;
using System.Text.Json;

namespace Sddp.Abstractions.Interfaces.Snapshots;

/// <summary>Optional module persistence participating in the core snapshot transaction.</summary>
public interface IProjectSnapshotExtension
{
    string Key { get; }
    Task LockAsync(IDbConnection connection, IDbTransaction transaction, Guid tenantId, Guid projectId, CancellationToken cancellationToken);
    Task EnsureIdleAsync(IDbConnection connection, IDbTransaction transaction, Guid tenantId, Guid projectId, CancellationToken cancellationToken);
    Task<ProjectSnapshotExtensionData> CaptureAsync(IDbConnection connection, IDbTransaction transaction, Guid tenantId, Guid projectId, CancellationToken cancellationToken);
    Task ValidateRestoreAsync(IDbConnection connection, IDbTransaction transaction, Guid tenantId, Guid projectId,
        ProjectSnapshotExtensionData? data, ProjectSnapshotData snapshot, CancellationToken cancellationToken);
    Task DeleteAsync(IDbConnection connection, IDbTransaction transaction, Guid tenantId, Guid projectId, bool preserveIdentity, CancellationToken cancellationToken);
    Task RestoreAsync(IDbConnection connection, IDbTransaction transaction, Guid tenantId, Guid projectId,
        ProjectSnapshotExtensionData data, CancellationToken cancellationToken);
}

public sealed class ProjectSnapshotExtensionData
{
    public int Version { get; set; }
    public JsonElement Data { get; set; }
}
