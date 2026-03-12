using Sddp.Abstractions.DTOs;

namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// notification
/// SignalR new
/// </summary>
public interface ITimelineNotificationService
{
    /// <summary>
    /// new create notification
    /// </summary>
    Task NotifyActivityCreatedAsync(
        string tenantId,
        string? projectId,
        AuditLogDto activity,
        CancellationToken cancellationToken = default);
}
