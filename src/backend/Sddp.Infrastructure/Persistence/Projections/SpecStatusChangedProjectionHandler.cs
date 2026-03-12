using Microsoft.Extensions.Logging;
using Sddp.Abstractions.Interfaces;
using Sddp.Domain.Events;

namespace Sddp.Infrastructure.Persistence.Projections;

/// <summary>
/// Spec status change
/// SignalR notification
/// </summary>
public class SpecStatusChangedProjectionHandler : IProjectionHandler<SpecStatusChangedEvent>
{
    private readonly ISpecNotificationService _notificationService;
    private readonly ILogger<SpecStatusChangedProjectionHandler> _logger;

    public SpecStatusChangedProjectionHandler(
        ISpecNotificationService notificationService,
        ILogger<SpecStatusChangedProjectionHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(SpecStatusChangedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing SpecStatusChanged: Spec {SpecId} {FromStatus} → {ToStatus}",
            @event.SpecId, @event.FromStatus, @event.ToStatus);

        await (_notificationService.NotifySpecStatusChangedAsync(
            @event.TenantId.ToString(),
            @event.ProjectId.ToString(),
            @event.SpecId.ToString(),
            @event.FromStatus.ToString(),
            @event.ToStatus.ToString(),
            @event.ActorId.ToString(),
            @event.Reason,
            cancellationToken)).ConfigureAwait(false);
    }
}
