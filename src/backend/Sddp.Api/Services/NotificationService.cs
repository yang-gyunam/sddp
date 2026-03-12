using Microsoft.AspNetCore.SignalR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Hubs;
using Sddp.Domain.Entities;

namespace Sddp.Api.Services;

/// <summary>
/// Notification creation service implementation.
/// Persists notifications and pushes them through SignalR in real time.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork unitOfWork,
        IHubContext<DashboardHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task CreateNotificationAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId recipientId,
        GlobalUniqueId? actorId,
        string type,
        string title,
        string message,
        string? entityType = null,
        GlobalUniqueId? entityId = null,
        CancellationToken ct = default)
    {
        var notificationRepo = _unitOfWork.Repository<Notification>();

        var notification = new Notification(
            tenantId,
            recipientId,
            actorId,
            type,
            title,
            message,
            entityType,
            entityId);

        await notificationRepo.AddAsync(notification, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Resolve actor name for the DTO
        string? actorName = null;
        if (actorId.HasValue)
        {
            var userRepo = _unitOfWork.Repository<User>();
            var actor = await userRepo.GetByIdAsync(actorId.Value, ct);
            actorName = actor?.DisplayName;
        }

        // Build DTO for SignalR push
        var dto = new NotificationDto(
            Id: notification.Id.ToString(),
            Type: type,
            Title: title,
            Message: message,
            IsRead: false,
            EntityType: entityType,
            EntityId: entityId?.ToString(),
            ActorName: actorName,
            CreatedAt: notification.CreatedAt.ToDateTimeOffset());

        // Push to recipient's personal dashboard group
        var groupName = $"dashboard:my:{recipientId}";
        try
        {
            await _hubContext.Clients.Group(groupName).SendAsync("NewNotification", dto, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to push notification via SignalR to {GroupName}", groupName);
        }
    }
}
