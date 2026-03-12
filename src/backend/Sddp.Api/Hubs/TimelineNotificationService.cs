using Microsoft.AspNetCore.SignalR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;

namespace Sddp.Api.Hubs;

/// <summary>
/// SignalR-based timeline activity notification service.
/// Broadcasts activity updates to system subscribers through DashboardHub.
/// </summary>
public class TimelineNotificationService : ITimelineNotificationService
{
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly ILogger<TimelineNotificationService> _logger;

    public TimelineNotificationService(
        IHubContext<DashboardHub> hubContext,
        ILogger<TimelineNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyActivityCreatedAsync(
        string tenantId,
        string? projectId,
        AuditLogDto activity,
        CancellationToken cancellationToken = default)
    {
        // Send to system dashboard subscribers.
        await _hubContext.Clients.Group("dashboard:system")
            .SendAsync("ActivityUpdated", activity, cancellationToken);

        // Also send to project subscribers.
        if (!string.IsNullOrWhiteSpace(projectId))
        {
            var projectGroup = $"dashboard:project:{projectId}";
            await _hubContext.Clients.Group(projectGroup)
                .SendAsync("ActivityUpdated", activity, cancellationToken);
        }

        _logger.LogDebug(
            "ActivityUpdated notification sent: {Action} {ResourceType} {ResourceId} (Tenant: {TenantId})",
            activity.Action, activity.ResourceType, activity.ResourceId, tenantId);
    }
}
