using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Conversations.Commands;

// ================================================================
// 1. ToggleStarredCommand
// ================================================================

/// <summary>
/// conversation
/// </summary>
public sealed record ToggleStarredCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId UserId) : ICommand<ToggleStarredResult>, IAuditableRequest<ToggleStarredResult>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ToggleStarredResult response) => AuditLog;
}

public sealed record ToggleStarredResult(bool IsStarred);

public sealed class ToggleStarredCommandHandler : IRequestHandler<ToggleStarredCommand, ToggleStarredResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public ToggleStarredCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ToggleStarredResult> Handle(ToggleStarredCommand request, CancellationToken cancellationToken)
    {
        var settingsRepo = _unitOfWork.Repository<UserConversationSettings>();

        var existing = await (settingsRepo.FindAsync(
            s => s.UserId == request.UserId
                && s.ConversationId == request.ConversationId
                && s.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var settings = existing.FirstOrDefault();

        if (settings is null)
        {
            settings = new UserConversationSettings(request.TenantId, request.UserId, request.ConversationId);
            settings.ToggleStarred();
            await (settingsRepo.AddAsync(settings, cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            settings.ToggleStarred();
            await (settingsRepo.UpdateAsync(settings, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: "toggle_starred",
            ResourceType: "conversation",
            ResourceId: request.ConversationId,
            Payload: new { IsStarred = settings.IsStarred },
            TenantId: request.TenantId,
            ProjectId: null);

        return new ToggleStarredResult(settings.IsStarred);
    }
}

// ================================================================
// 2. ToggleMutedCommand
// ================================================================

/// <summary>
/// conversation
/// </summary>
public sealed record ToggleMutedCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId UserId) : ICommand<ToggleMutedResult>, IAuditableRequest<ToggleMutedResult>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ToggleMutedResult response) => AuditLog;
}

public sealed record ToggleMutedResult(bool IsMuted);

public sealed class ToggleMutedCommandHandler : IRequestHandler<ToggleMutedCommand, ToggleMutedResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public ToggleMutedCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ToggleMutedResult> Handle(ToggleMutedCommand request, CancellationToken cancellationToken)
    {
        var settingsRepo = _unitOfWork.Repository<UserConversationSettings>();

        var existing = await (settingsRepo.FindAsync(
            s => s.UserId == request.UserId
                && s.ConversationId == request.ConversationId
                && s.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var settings = existing.FirstOrDefault();

        if (settings is null)
        {
            settings = new UserConversationSettings(request.TenantId, request.UserId, request.ConversationId);
            settings.ToggleMuted();
            await (settingsRepo.AddAsync(settings, cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            settings.ToggleMuted();
            await (settingsRepo.UpdateAsync(settings, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: "toggle_muted",
            ResourceType: "conversation",
            ResourceId: request.ConversationId,
            Payload: new { IsMuted = settings.IsMuted },
            TenantId: request.TenantId,
            ProjectId: null);

        return new ToggleMutedResult(settings.IsMuted);
    }
}

// ================================================================
// 3. GetUserConversationSettingsQuery
// ================================================================

/// <summary>
/// user conversation settings get (,, read status)
/// </summary>
public sealed record GetUserConversationSettingsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId UserId) : ICommand<UserConversationSettingsDto>;

public sealed class GetUserConversationSettingsQueryHandler : IRequestHandler<GetUserConversationSettingsQuery, UserConversationSettingsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserConversationSettingsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserConversationSettingsDto> Handle(GetUserConversationSettingsQuery request, CancellationToken cancellationToken)
    {
        var settingsRepo = _unitOfWork.Repository<UserConversationSettings>();
        var readStatusRepo = _unitOfWork.Repository<UserReadStatus>();

        var settingsResult = await (settingsRepo.FindAsync(
            s => s.UserId == request.UserId
                && s.ConversationId == request.ConversationId
                && s.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var settings = settingsResult.FirstOrDefault();

        var readStatusResult = await (readStatusRepo.FindAsync(
            rs => rs.UserId == request.UserId
                && rs.ConversationId == request.ConversationId,
            cancellationToken)).ConfigureAwait(false);
        var readStatus = readStatusResult.FirstOrDefault();

        return new UserConversationSettingsDto(
            ConversationId: request.ConversationId.ToString(),
            IsStarred: settings?.IsStarred ?? false,
            IsMuted: settings?.IsMuted ?? false,
            LastReadAt: readStatus?.LastReadAt?.ToDateTimeOffset());
    }
}
