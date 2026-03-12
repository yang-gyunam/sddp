using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Notifications;

/// <summary>
/// notification get ()
/// </summary>
public sealed record GetMyNotificationsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId,
    int Page = 1,
    int PageSize = 20) : IQuery<MyNotificationsDto>;

public sealed class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, MyNotificationsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMyNotificationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MyNotificationsDto> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notificationRepo = _unitOfWork.Repository<Notification>();
        var userRepo = _unitOfWork.Repository<User>();

        // read count (all)
        var unreadCount = await (notificationRepo.CountAsync(
            n => n.TenantId == request.TenantId
                && n.RecipientId == request.UserId
                && !n.IsRead,
            cancellationToken)).ConfigureAwait(false);

        // DB
        var (paged, _) = await (notificationRepo.FindPagedAsync(
            n => n.TenantId == request.TenantId
                && n.RecipientId == request.UserId,
            request.Page, request.PageSize,
            q => q.OrderByDescending(n => n.CreatedAt),
            cancellationToken)).ConfigureAwait(false);

        // actor get
        var actorIds = paged
            .Where(n => n.ActorId.HasValue)
            .Select(n => n.ActorId!.Value)
            .Distinct()
            .ToList();

        var actors = actorIds.Count == 0
            ? Array.Empty<User>()
            : await (userRepo.FindAsync(u => actorIds.Contains(u.Id), cancellationToken)).ConfigureAwait(false);
        var actorNameMap = actors.ToDictionary(u => u.Id, u => u.DisplayName);

        var dtos = paged.Select(n => new NotificationDto(
            Id: n.Id.ToString(),
            Type: n.Type,
            Title: n.Title,
            Message: n.Message,
            IsRead: n.IsRead,
            EntityType: n.EntityType,
            EntityId: n.EntityId?.ToString(),
            ActorName: n.ActorId.HasValue && actorNameMap.TryGetValue(n.ActorId.Value, out var name) ? name : null,
            CreatedAt: n.CreatedAt.ToDateTimeOffset()
        )).ToList();

        return new MyNotificationsDto(dtos, unreadCount);
    }
}
