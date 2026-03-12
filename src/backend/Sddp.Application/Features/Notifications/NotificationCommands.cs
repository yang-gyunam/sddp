using MediatR;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Notifications;

/// <summary>
/// notification mark as read
/// </summary>
public sealed record MarkNotificationReadCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId NotificationId,
    GlobalUniqueId UserId) : ICommand<bool>;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationReadCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notificationRepo = _unitOfWork.Repository<Notification>();

        var notification = await (notificationRepo.GetByIdAsync(request.NotificationId, cancellationToken)).ConfigureAwait(false);
        if (notification is null
            || notification.TenantId != request.TenantId
            || notification.RecipientId != request.UserId
            || !notification.IsActive)
        {
            return false;
        }

        notification.MarkAsRead();
        await (notificationRepo.UpdateAsync(notification, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        return true;
    }
}

/// <summary>
/// all notification mark as read
/// </summary>
public sealed record MarkAllNotificationsReadCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId) : ICommand<int>;

public sealed class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public MarkAllNotificationsReadCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var notificationRepo = _unitOfWork.Repository<Notification>();

        var unreadNotifications = await (notificationRepo.FindAsync(
            n => n.TenantId == request.TenantId
                && n.RecipientId == request.UserId
                && n.IsActive
                && !n.IsRead,
            cancellationToken)).ConfigureAwait(false);

        var count = 0;
        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead();
            await (notificationRepo.UpdateAsync(notification, cancellationToken)).ConfigureAwait(false);
            count++;
        }

        if (count > 0)
        {
            await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
        }

        return count;
    }
}
