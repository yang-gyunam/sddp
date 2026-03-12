using Microsoft.AspNetCore.SignalR;
using Sddp.Abstractions.Interfaces;

namespace Sddp.Api.Hubs;

/// <summary>
/// SignalR-based Spec status change notification service.
/// Broadcasts status updates to project subscribers through DashboardHub.
/// </summary>
public class SpecNotificationService : ISpecNotificationService
{
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly ILogger<SpecNotificationService> _logger;

    public SpecNotificationService(
        IHubContext<DashboardHub> hubContext,
        ILogger<SpecNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifySpecStatusChangedAsync(
        string tenantId,
        string projectId,
        string specId,
        string fromStatus,
        string toStatus,
        string actorId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            specId,
            tenantId,
            projectId,
            fromStatus,
            toStatus,
            actorId,
            reason,
            timestamp = DateTimeOffset.UtcNow
        };

        // Send to project subscribers.
        var projectGroup = $"dashboard:project:{projectId}";
        await _hubContext.Clients.Group(projectGroup)
            .SendAsync("SpecStatusChanged", payload, cancellationToken);

        // Also send to system dashboard subscribers.
        await _hubContext.Clients.Group("dashboard:system")
            .SendAsync("SpecStatusChanged", payload, cancellationToken);

        _logger.LogInformation(
            "SpecStatusChanged notification sent: Spec {SpecId} {FromStatus} → {ToStatus} (Project: {ProjectId})",
            specId, fromStatus, toStatus, projectId);
    }
}
