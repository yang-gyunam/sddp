using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Conversations.Commands;

// =============================================================================
// ConcludeChannelCommand — Active → Concluded
// =============================================================================

/// <summary>
/// channel (Active → Concluded)
/// </summary>
public sealed record ConcludeChannelCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId RequestUserId,
    GlobalUniqueId? DecisionSpecId) : ICommand<ConversationSummaryDto?>, IAuditableRequest<ConversationSummaryDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ConversationSummaryDto? response) => AuditLog;
}

public sealed class ConcludeChannelCommandHandler : IRequestHandler<ConcludeChannelCommand, ConversationSummaryDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConversationHubService _hubService;

    public ConcludeChannelCommandHandler(IUnitOfWork unitOfWork, IConversationHubService hubService)
    {
        _unitOfWork = unitOfWork;
        _hubService = hubService;
    }

    public async Task<ConversationSummaryDto?> Handle(ConcludeChannelCommand request, CancellationToken cancellationToken)
    {
        var channelRepo = _unitOfWork.Repository<Channel>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
        var userRepo = _unitOfWork.Repository<User>();

        var channel = await (channelRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (channel is null || channel.TenantId != request.TenantId || !channel.IsActive)
        {
            return null;
        }

        // permission: Owner(create) conclude
        var requesterMembers = await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId
                && m.UserId == request.RequestUserId
                && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var requester = requesterMembers.FirstOrDefault();
        if (requester is null || !requester.Role.CanManageConversation())
        {
            throw new SddpException("CONCLUDE_FAILED", "Only the channel owner can conclude channels");
        }

        var concludeResult = channel.Conclude(request.DecisionSpecId);
        concludeResult.EnsureSuccess("CONCLUDE_FAILED");
        await (channelRepo.UpdateAsync(channel, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // SignalR: currently participating members get immediate readonly-state sync in open views.
        var requesterUser = await (userRepo.GetByIdAsync(request.RequestUserId, cancellationToken)).ConfigureAwait(false);
        var requesterName = requesterUser?.DisplayName ?? "Unknown";
        await (_hubService.BroadcastConversationClosedAsync(
            request.ConversationId.ToString(),
            request.DecisionSpecId?.ToString(),
            requesterName)).ConfigureAwait(false);

        var memberCount = (await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId && m.IsActive,
            cancellationToken)).ConfigureAwait(false)).Count;

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "conclude",
            ResourceType: "channel",
            ResourceId: request.ConversationId,
            Payload: new { channel.Name, channel.Visibility, DecisionSpecId = request.DecisionSpecId?.ToString() },
            TenantId: request.TenantId,
            ProjectId: null);

        return ConversationMapping.MapConversationSummaryDto(channel, memberCount, 0, null);
    }
}

// =============================================================================
// ReopenChannelCommand — Concluded → Active
// =============================================================================

/// <summary>
/// channel (Concluded → Active)
/// </summary>
public sealed record ReopenChannelCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId RequestUserId) : ICommand<ConversationSummaryDto?>, IAuditableRequest<ConversationSummaryDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ConversationSummaryDto? response) => AuditLog;
}

public sealed class ReopenChannelCommandHandler : IRequestHandler<ReopenChannelCommand, ConversationSummaryDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public ReopenChannelCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConversationSummaryDto?> Handle(ReopenChannelCommand request, CancellationToken cancellationToken)
    {
        var channelRepo = _unitOfWork.Repository<Channel>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();

        var channel = await (channelRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (channel is null || channel.TenantId != request.TenantId || !channel.IsActive)
        {
            return null;
        }

        var requesterMembers = await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId
                && m.UserId == request.RequestUserId
                && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var requester = requesterMembers.FirstOrDefault();
        if (requester is null || !requester.Role.CanManageMembers())
        {
            throw new SddpException("REOPEN_FAILED", "Only owners and moderators can reopen channels");
        }

        var reopenResult = channel.Reopen();
        reopenResult.EnsureSuccess("REOPEN_FAILED");
        await (channelRepo.UpdateAsync(channel, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var memberCount = (await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId && m.IsActive,
            cancellationToken)).ConfigureAwait(false)).Count;

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "reopen",
            ResourceType: "channel",
            ResourceId: request.ConversationId,
            Payload: new { channel.Name, channel.Visibility },
            TenantId: request.TenantId,
            ProjectId: null);

        return ConversationMapping.MapConversationSummaryDto(channel, memberCount, 0, null);
    }
}

// =============================================================================
// ArchiveChannelCommand — Concluded → Archived
// =============================================================================

/// <summary>
/// channel (Concluded → Archived)
/// </summary>
public sealed record ArchiveChannelCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId RequestUserId) : ICommand<ConversationSummaryDto?>, IAuditableRequest<ConversationSummaryDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ConversationSummaryDto? response) => AuditLog;
}

public sealed class ArchiveChannelCommandHandler : IRequestHandler<ArchiveChannelCommand, ConversationSummaryDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public ArchiveChannelCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConversationSummaryDto?> Handle(ArchiveChannelCommand request, CancellationToken cancellationToken)
    {
        var channelRepo = _unitOfWork.Repository<Channel>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();

        var channel = await (channelRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (channel is null || channel.TenantId != request.TenantId || !channel.IsActive)
        {
            return null;
        }

        var requesterMembers = await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId
                && m.UserId == request.RequestUserId
                && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var requester = requesterMembers.FirstOrDefault();
        if (requester is null || !requester.Role.CanManageMembers())
        {
            throw new SddpException("ARCHIVE_FAILED", "Only owners and moderators can archive channels");
        }

        var archiveResult = channel.Archive();
        archiveResult.EnsureSuccess("ARCHIVE_FAILED");
        await (channelRepo.UpdateAsync(channel, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var memberCount = (await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId && m.IsActive,
            cancellationToken)).ConfigureAwait(false)).Count;

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "archive",
            ResourceType: "channel",
            ResourceId: request.ConversationId,
            Payload: new { channel.Name, channel.Visibility },
            TenantId: request.TenantId,
            ProjectId: null);

        return ConversationMapping.MapConversationSummaryDto(channel, memberCount, 0, null);
    }
}
