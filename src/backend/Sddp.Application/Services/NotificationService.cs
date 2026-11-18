using Sddp.Abstractions.Events;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Services;

public sealed class NotificationService(IUnitOfWork unitOfWork, IOutboxEnqueuer outbox) : INotificationService
{
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
        var notificationRepo = unitOfWork.Repository<Notification>();

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
        // Enqueue flushes both tracked rows in the same unit of work.
        await outbox.EnqueueAsync(new NotificationCreatedEvent(), "notification", notification.Id, ct);
    }
}
