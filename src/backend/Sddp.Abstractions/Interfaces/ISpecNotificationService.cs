namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// Real-time notification service for Spec status changes.
/// Broadcasts status updates to subscribers through SignalR.
/// </summary>
public interface ISpecNotificationService
{
    /// <summary>
    /// Sends a notification for a Spec status change.
    /// </summary>
    Task NotifySpecStatusChangedAsync(
        string tenantId,
        string projectId,
        string specId,
        string fromStatus,
        string toStatus,
        string actorId,
        string? reason = null,
        CancellationToken cancellationToken = default);
}
