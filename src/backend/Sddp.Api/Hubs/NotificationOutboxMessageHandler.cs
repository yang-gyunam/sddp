using Microsoft.AspNetCore.SignalR;
using Sddp.Abstractions.Events;
using Sddp.Abstractions.Interfaces;
using Sddp.Application.Features.Notifications;
using Sddp.Domain.Entities;
using Sddp.Infrastructure.Persistence.Outbox;

namespace Sddp.Api.Hubs;

public sealed class NotificationOutboxMessageHandler(IUnitOfWork unitOfWork, IHubContext<DashboardHub> hub)
    : IOutboxMessageHandler
{
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        if (message.EventType != nameof(NotificationCreatedEvent)) return;
        var notification = await unitOfWork.Repository<Notification>().GetByIdAsync(message.AggregateId, cancellationToken);
        if (notification is null) return;

        var actor = notification.ActorId is { } actorId
            ? await unitOfWork.Repository<User>().GetByIdAsync(actorId, cancellationToken) : null;
        await hub.Clients.Group($"dashboard:my:{notification.RecipientId}").SendAsync(
            "NewNotification", NotificationMapping.ToDto(notification, actor?.DisplayName), cancellationToken);
    }
}
