using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// audit log
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// audit log
    /// </summary>
    Task LogAsync(
        GlobalUniqueId? actorId,
        string action,
        string resourceType,
        GlobalUniqueId resourceId,
        object? payload = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// audit log (tenant/project)
    /// </summary>
    Task LogAsync(
        GlobalUniqueId? actorId,
        string action,
        string resourceType,
        GlobalUniqueId resourceId,
        object? payload,
        GlobalUniqueId? tenantId,
        GlobalUniqueId? projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// audit log get
    /// </summary>
    Task<IReadOnlyList<AuditLogEntry>> GetByResourceAsync(
        string resourceType,
        GlobalUniqueId resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// user audit log get
    /// </summary>
    Task<IReadOnlyList<AuditLogEntry>> GetByActorAsync(
        GlobalUniqueId actorId,
        CancellationToken cancellationToken = default);
}
