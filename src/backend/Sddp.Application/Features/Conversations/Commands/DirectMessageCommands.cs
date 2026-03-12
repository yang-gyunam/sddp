using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Conversations.Commands;

internal static class DirectMessageCommandHelper
{
    internal static async Task EnsureProjectMembersAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId projectId,
        IEnumerable<GlobalUniqueId> userIds,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var distinctUserIds = userIds
            .Where(id => !id.IsEmpty)
            .Distinct()
            .ToList();
        if (distinctUserIds.Count == 0)
        {
            return;
        }

        var projectMemberRepo = unitOfWork.Repository<ProjectMember>();
        var userGuids = distinctUserIds.Select(id => id.ToGuid()).ToList();
        var projectMembers = await (projectMemberRepo.FindAsync(
            m => m.ProjectId == projectId
                && m.IsActive
                && userGuids.Contains((Guid)m.UserId),
            cancellationToken)).ConfigureAwait(false);

        var memberUserIds = projectMembers
            .Select(member => member.UserId)
            .ToHashSet();
        if (distinctUserIds.Any(userId => !memberUserIds.Contains(userId)))
        {
            throw new SddpException(errorCode, errorMessage);
        }
    }

    internal static async Task<(Conversation Conversation, User CurrentUser, User TargetUser, bool IsCreated, int MemberCount)> GetOrCreateAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId userId,
        GlobalUniqueId targetUserId,
        GlobalUniqueId? projectId,
        CancellationToken cancellationToken)
    {
        if (userId == targetUserId)
        {
            throw new SddpException("DM_ERROR", "Cannot create a DM with yourself");
        }

        var conversationRepo = unitOfWork.Repository<Conversation>();
        var memberRepo = unitOfWork.Repository<ConversationMember>();
        var userRepo = unitOfWork.Repository<User>();

        var targetUser = await (userRepo.GetByIdAsync(targetUserId, cancellationToken)).ConfigureAwait(false);
        if (targetUser is null || !targetUser.IsActive)
        {
            throw new SddpException("DM_ERROR", "Target user not found");
        }

        var currentUser = await (userRepo.GetByIdAsync(userId, cancellationToken)).ConfigureAwait(false);
        if (currentUser is null || !currentUser.IsActive)
        {
            throw new SddpException("DM_ERROR", "Current user not found");
        }

        if (projectId.HasValue)
        {
            await (EnsureProjectMembersAsync(
                unitOfWork,
                projectId.Value,
                [userId, targetUserId],
                "DM_ERROR",
                "Only project members can start project direct messages",
                cancellationToken)).ConfigureAwait(false);
        }

        var userMemberships = await (memberRepo.FindAsync(
            m => m.UserId == userId && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var userConversationIds = userMemberships.Select(m => m.ConversationId).ToList();

        if (userConversationIds.Count > 0)
        {
            var userConversationGuids = userConversationIds.Select(id => id.ToGuid()).ToList();

            var dmConversations = await (conversationRepo.FindAsync(
                c => c.TenantId == tenantId
                    && c.ConversationType == ConversationType.DirectMessage
                    && c.IsActive
                    && c.ProjectId == projectId
                    && userConversationGuids.Contains((Guid)c.Id),
                cancellationToken)).ConfigureAwait(false);

            var dmIds = dmConversations.Select(d => d.Id).ToList();
            if (dmIds.Count > 0)
            {
                var dmGuids = dmIds.Select(id => id.ToGuid()).ToList();

                var targetMemberships = await (memberRepo.FindAsync(
                    m => m.UserId == targetUserId
                        && m.IsActive
                        && dmGuids.Contains((Guid)m.ConversationId),
                    cancellationToken)).ConfigureAwait(false);

                if (targetMemberships.Any())
                {
                    var existingDmId = targetMemberships.First().ConversationId;
                    var existingDm = dmConversations.First(c => c.Id == existingDmId);

                    // Concluded/Archived DM ()
                    // active, DM Active status DM
                    var dmRepo = unitOfWork.Repository<DirectMessage>();
                    var dmEntity = await (dmRepo.GetByIdAsync(existingDmId, cancellationToken)).ConfigureAwait(false);
                    if (dmEntity is not null && dmEntity.Status != DirectMessageStatus.Active)
                    {
                        // Concluded/Archived → new DM create progress (create)
                    }
                    else
                    {
                        var memberCount = (await (memberRepo.FindAsync(
                            m => m.ConversationId == existingDmId && m.IsActive,
                            cancellationToken)).ConfigureAwait(false)).Count();

                        return (existingDm, currentUser, targetUser, false, memberCount);
                    }
                }
            }
        }

        var dmName = $"{currentUser.DisplayName} <-> {targetUser.DisplayName}";
        var dm = new DirectMessage(
            tenantId,
            projectId: projectId,
            name: dmName);

        await (conversationRepo.AddAsync(dm, cancellationToken)).ConfigureAwait(false);

        var ownerMember = new ConversationMember(
            dm.Id,
            userId,
            ConversationMemberRole.Owner,
            ParticipantType.Human);
        var targetMember = new ConversationMember(
            dm.Id,
            targetUserId,
            ConversationMemberRole.Member,
            ParticipantType.Human);

        await (memberRepo.AddAsync(ownerMember, cancellationToken)).ConfigureAwait(false);
        await (memberRepo.AddAsync(targetMember, cancellationToken)).ConfigureAwait(false);
        await (unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        return (dm, currentUser, targetUser, true, 2);
    }
}

/// <summary>
/// DM get new DM create
/// </summary>
public sealed record GetOrCreateDirectMessageCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId,
    GlobalUniqueId TargetUserId,
    GlobalUniqueId? ProjectId = null) : ICommand<ConversationSummaryDto>, IAuditableRequest<ConversationSummaryDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ConversationSummaryDto response) => AuditLog;
}

public sealed class GetOrCreateDirectMessageCommandHandler : IRequestHandler<GetOrCreateDirectMessageCommand, ConversationSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetOrCreateDirectMessageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConversationSummaryDto> Handle(GetOrCreateDirectMessageCommand request, CancellationToken cancellationToken)
    {
        var result = await (DirectMessageCommandHelper.GetOrCreateAsync(
            _unitOfWork,
            request.TenantId,
            request.UserId,
            request.TargetUserId,
            request.ProjectId,
            cancellationToken)).ConfigureAwait(false);

        if (result.IsCreated)
        {
            request.AuditLog = new AuditLogRequest(
                ActorId: request.UserId,
                Action: "create",
                ResourceType: "direct_message",
                ResourceId: result.Conversation.Id,
                Payload: new
                {
                    TargetUserId = request.TargetUserId.ToString(),
                    TargetUserName = result.TargetUser.DisplayName
                },
                TenantId: request.TenantId,
                ProjectId: request.ProjectId);
        }

        return ConversationMapping.MapConversationSummaryDto(
            result.Conversation,
            memberCount: result.MemberCount,
            unreadCount: 0,
            lastMessageAt: null);
    }
}

// =============================================================================
// DirectMessage Invitation
// =============================================================================

public sealed record DirectMessageInvitationResultDto(
    bool Sent,
    string Message);

/// <summary>
/// DM
/// </summary>
public sealed record SendDirectMessageInvitationCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId InviterUserId,
    GlobalUniqueId TargetUserId,
    GlobalUniqueId? ProjectId = null) : ICommand<DirectMessageInvitationResultDto>, IAuditableRequest<DirectMessageInvitationResultDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(DirectMessageInvitationResultDto response) => AuditLog;
}

public sealed class SendDirectMessageInvitationCommandHandler : IRequestHandler<SendDirectMessageInvitationCommand, DirectMessageInvitationResultDto>
{
    private const string InviteNotificationType = "dm_invite";
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public SendDirectMessageInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<DirectMessageInvitationResultDto> Handle(SendDirectMessageInvitationCommand request, CancellationToken cancellationToken)
    {
        if (request.InviterUserId == request.TargetUserId)
        {
            throw new SddpException("DM_INVITE_FAILED", "Cannot invite yourself to direct message");
        }

        var userRepo = _unitOfWork.Repository<User>();
        var notificationRepo = _unitOfWork.Repository<Notification>();

        var inviter = await (userRepo.GetByIdAsync(request.InviterUserId, cancellationToken)).ConfigureAwait(false);
        var targetUser = await (userRepo.GetByIdAsync(request.TargetUserId, cancellationToken)).ConfigureAwait(false);
        if (inviter is null || !inviter.IsActive || targetUser is null || !targetUser.IsActive)
        {
            throw new SddpException("DM_INVITE_FAILED", "User not found");
        }

        if (request.ProjectId.HasValue)
        {
            await (DirectMessageCommandHelper.EnsureProjectMembersAsync(
                _unitOfWork,
                request.ProjectId.Value,
                [request.InviterUserId, request.TargetUserId],
                "DM_INVITE_FAILED",
                "Both users must be active project members for project direct messages",
                cancellationToken)).ConfigureAwait(false);
        }

        // Check for existing active DM between the two users
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
        var dmRepo = _unitOfWork.Repository<DirectMessage>();

        var inviterMemberships = await (memberRepo.FindAsync(
            m => m.UserId == request.InviterUserId && m.IsActive, cancellationToken)).ConfigureAwait(false);
        var inviterConvIds = inviterMemberships.Select(m => m.ConversationId).ToList();

        if (inviterConvIds.Count > 0)
        {
            var inviterConvGuids = inviterConvIds.Select(id => id.ToGuid()).ToList();
            var dmConversations = await (conversationRepo.FindAsync(
                c => c.TenantId == request.TenantId
                    && c.ConversationType == ConversationType.DirectMessage
                    && c.IsActive
                    && c.ProjectId == request.ProjectId
                    && inviterConvGuids.Contains((Guid)c.Id),
                cancellationToken)).ConfigureAwait(false);

            var dmIds = dmConversations.Select(d => d.Id).ToList();
            if (dmIds.Count > 0)
            {
                var dmGuids = dmIds.Select(id => id.ToGuid()).ToList();
                var sharedDmIds = (await (memberRepo.FindAsync(
                    m => m.UserId == request.TargetUserId
                        && m.IsActive
                        && dmGuids.Contains((Guid)m.ConversationId),
                    cancellationToken)).ConfigureAwait(false)).Select(m => m.ConversationId.ToGuid()).ToList();

                if (sharedDmIds.Count > 0)
                {
                    var activeDmExists = (await (dmRepo.FindAsync(
                        d => sharedDmIds.Contains((Guid)d.Id)
                            && d.Status == DirectMessageStatus.Active,
                        cancellationToken)).ConfigureAwait(false)).Any();

                    if (activeDmExists)
                    {
                        return new DirectMessageInvitationResultDto(
                            Sent: false,
                            Message: "Already have an active conversation with this user.");
                    }
                }
            }
        }

        var existingInvite = (await (notificationRepo.FindAsync(
            n => n.TenantId == request.TenantId
                && n.Type == InviteNotificationType
                && !n.IsRead
                && n.IsActive
                && n.EntityId == request.ProjectId
                && (
                    // A→B
                    (n.RecipientId == request.TargetUserId && n.ActorId == request.InviterUserId)
                    // B→A ()
                    || (n.RecipientId == request.InviterUserId && n.ActorId == request.TargetUserId)
                ),
            cancellationToken)).ConfigureAwait(false)).Any();

        if (existingInvite)
        {
            return new DirectMessageInvitationResultDto(
                Sent: false,
                Message: "A pending invitation already exists.");
        }

        await (_notificationService.CreateNotificationAsync(
            request.TenantId,
            request.TargetUserId,
            request.InviterUserId,
            type: InviteNotificationType,
            title: "Direct message invitation",
            message: $"{inviter.DisplayName} invited you to start a direct message.",
            entityType: "direct_message_invite",
            entityId: request.ProjectId,
            ct: cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.InviterUserId,
            Action: "invite",
            ResourceType: "direct_message",
            ResourceId: request.TargetUserId,
            Payload: new
            {
                TargetUserId = request.TargetUserId.ToString(),
                TargetUserName = targetUser.DisplayName
            },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return new DirectMessageInvitationResultDto(
            Sent: true,
            Message: "Invitation sent.");
    }
}

/// <summary>
/// DM
/// </summary>
public sealed record AcceptDirectMessageInvitationCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId NotificationId,
    GlobalUniqueId RecipientUserId) : ICommand<ConversationSummaryDto?>, IAuditableRequest<ConversationSummaryDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ConversationSummaryDto? response) => AuditLog;
}

public sealed class AcceptDirectMessageInvitationCommandHandler : IRequestHandler<AcceptDirectMessageInvitationCommand, ConversationSummaryDto?>
{
    private const string AcceptedNotificationType = "dm_invite_accepted";
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public AcceptDirectMessageInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<ConversationSummaryDto?> Handle(AcceptDirectMessageInvitationCommand request, CancellationToken cancellationToken)
    {
        var notificationRepo = _unitOfWork.Repository<Notification>();

        var invite = await (notificationRepo.GetByIdAsync(request.NotificationId, cancellationToken)).ConfigureAwait(false);
        if (invite is null
            || invite.TenantId != request.TenantId
            || invite.RecipientId != request.RecipientUserId
            || invite.Type != "dm_invite"
            || !invite.IsActive)
        {
            return null;
        }

        if (invite.IsRead)
        {
            throw new SddpException("DM_INVITE_ACCEPT_FAILED", "Invitation is already handled");
        }

        if (!invite.ActorId.HasValue)
        {
            throw new SddpException("DM_INVITE_ACCEPT_FAILED", "Invitation actor is missing");
        }

        var inviterUserId = invite.ActorId.Value;
        var projectId = invite.EntityId;

        var result = await (DirectMessageCommandHelper.GetOrCreateAsync(
            _unitOfWork,
            request.TenantId,
            request.RecipientUserId,
            inviterUserId,
            projectId,
            cancellationToken)).ConfigureAwait(false);

        invite.MarkAsRead();
        await (notificationRepo.UpdateAsync(invite, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var projectScope = projectId?.ToString() ?? string.Empty;
        var entityType = string.IsNullOrWhiteSpace(projectScope)
            ? "direct_message"
            : $"dm:{projectScope}";

        var existingAcceptedNotification = (await (notificationRepo.FindAsync(
            n => n.TenantId == request.TenantId
                && n.RecipientId == inviterUserId
                && n.ActorId == request.RecipientUserId
                && n.Type == AcceptedNotificationType
                && n.EntityId == result.Conversation.Id
                && n.IsActive
                && !n.IsRead,
            cancellationToken)).ConfigureAwait(false)).Any();

        if (!existingAcceptedNotification)
        {
            await (_notificationService.CreateNotificationAsync(
                request.TenantId,
                inviterUserId,
                request.RecipientUserId,
                type: AcceptedNotificationType,
                title: "Direct message invitation accepted",
                message: $"{result.CurrentUser.DisplayName} accepted your direct message invitation.",
                entityType: entityType,
                entityId: result.Conversation.Id,
                ct: cancellationToken)).ConfigureAwait(false);
        }

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RecipientUserId,
            Action: "accept_invite",
            ResourceType: "direct_message",
            ResourceId: result.Conversation.Id,
            Payload: new
            {
                NotificationId = request.NotificationId.ToString(),
                InviterUserId = inviterUserId.ToString()
            },
            TenantId: request.TenantId,
            ProjectId: projectId);

        return ConversationMapping.MapConversationSummaryDto(
            result.Conversation,
            memberCount: result.MemberCount,
            unreadCount: 0,
            lastMessageAt: null);
    }
}

/// <summary>
/// DM
/// </summary>
public sealed record RejectDirectMessageInvitationCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId NotificationId,
    GlobalUniqueId RecipientUserId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class RejectDirectMessageInvitationCommandHandler : IRequestHandler<RejectDirectMessageInvitationCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public RejectDirectMessageInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<bool> Handle(RejectDirectMessageInvitationCommand request, CancellationToken cancellationToken)
    {
        var notificationRepo = _unitOfWork.Repository<Notification>();
        var userRepo = _unitOfWork.Repository<User>();

        var invite = await (notificationRepo.GetByIdAsync(request.NotificationId, cancellationToken)).ConfigureAwait(false);
        if (invite is null
            || invite.TenantId != request.TenantId
            || invite.RecipientId != request.RecipientUserId
            || invite.Type != "dm_invite"
            || !invite.IsActive)
        {
            return false;
        }

        if (invite.IsRead)
        {
            throw new SddpException("DM_INVITE_REJECT_FAILED", "Invitation is already handled");
        }

        if (!invite.ActorId.HasValue)
        {
            throw new SddpException("DM_INVITE_REJECT_FAILED", "Invitation actor is missing");
        }

        var recipient = await (userRepo.GetByIdAsync(request.RecipientUserId, cancellationToken)).ConfigureAwait(false);
        var recipientName = recipient?.DisplayName ?? "User";

        invite.MarkAsRead();
        await (notificationRepo.UpdateAsync(invite, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        await (_notificationService.CreateNotificationAsync(
            request.TenantId,
            invite.ActorId.Value,
            request.RecipientUserId,
            type: "dm_invite_rejected",
            title: "Direct message invitation declined",
            message: $"{recipientName} declined your direct message invitation.",
            entityType: "direct_message_invite",
            entityId: invite.EntityId,
            ct: cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RecipientUserId,
            Action: "reject_invite",
            ResourceType: "direct_message_invite",
            ResourceId: request.NotificationId,
            Payload: new { InviterUserId = invite.ActorId.Value.ToString() },
            TenantId: request.TenantId,
            ProjectId: invite.EntityId);

        return true;
    }
}

// =============================================================================
// ConcludeDirectMessageCommand — Active → Concluded
// =============================================================================

/// <summary>
/// DM (Active → Concluded)
/// </summary>
public sealed record ConcludeDirectMessageCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId RequestUserId,
    GlobalUniqueId? DecisionSpecId) : ICommand<ConversationSummaryDto?>, IAuditableRequest<ConversationSummaryDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ConversationSummaryDto? response) => AuditLog;
}

public sealed class ConcludeDirectMessageCommandHandler : IRequestHandler<ConcludeDirectMessageCommand, ConversationSummaryDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConversationHubService _hubService;

    public ConcludeDirectMessageCommandHandler(IUnitOfWork unitOfWork, IConversationHubService hubService)
    {
        _unitOfWork = unitOfWork;
        _hubService = hubService;
    }

    public async Task<ConversationSummaryDto?> Handle(ConcludeDirectMessageCommand request, CancellationToken cancellationToken)
    {
        var dmRepo = _unitOfWork.Repository<DirectMessage>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
        var userRepo = _unitOfWork.Repository<User>();

        var dm = await (dmRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (dm is null || dm.TenantId != request.TenantId || !dm.IsActive)
        {
            return null;
        }

        // permission: DM conclude
        var allMembers = await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var allMembersList = allMembers.ToList();
        if (!allMembersList.Any(m => m.UserId == request.RequestUserId))
        {
            throw new SddpException("CONCLUDE_FAILED", "Only DM members can conclude direct messages");
        }

        var concludeResult = dm.Conclude(request.DecisionSpecId);
        concludeResult.EnsureSuccess("CONCLUDE_FAILED");

        // DM conclude is a status transition only (history is preserved).
        await (dmRepo.UpdateAsync(dm, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // SignalR: notify members about DM concluded
        var requester = await (userRepo.GetByIdAsync(request.RequestUserId, cancellationToken)).ConfigureAwait(false);
        var requesterName = requester?.DisplayName ?? "Unknown";
        var memberUserIds = allMembersList.Select(m => m.UserId.ToString()).ToList();
        var conversationIdStr = request.ConversationId.ToString();

        await (_hubService.BroadcastConversationClosedAsync(conversationIdStr, request.DecisionSpecId?.ToString(), requesterName)).ConfigureAwait(false);
        await (_hubService.NotifyDMConcludedAsync(memberUserIds, conversationIdStr, requesterName)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "conclude",
            ResourceType: "direct_message",
            ResourceId: request.ConversationId,
            Payload: new { dm.Name, DecisionSpecId = request.DecisionSpecId?.ToString() },
            TenantId: request.TenantId,
            ProjectId: null);

        return ConversationMapping.MapConversationSummaryDto(dm, allMembersList.Count, 0, null);
    }
}

// =============================================================================
// ReopenDirectMessageCommand — Concluded → Active
// =============================================================================

/// <summary>
/// DM (Concluded → Active)
/// </summary>
public sealed record ReopenDirectMessageCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId RequestUserId) : ICommand<ConversationSummaryDto?>, IAuditableRequest<ConversationSummaryDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ConversationSummaryDto? response) => AuditLog;
}

public sealed class ReopenDirectMessageCommandHandler : IRequestHandler<ReopenDirectMessageCommand, ConversationSummaryDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public ReopenDirectMessageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConversationSummaryDto?> Handle(ReopenDirectMessageCommand request, CancellationToken cancellationToken)
    {
        var dmRepo = _unitOfWork.Repository<DirectMessage>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();

        var dm = await (dmRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (dm is null || dm.TenantId != request.TenantId || !dm.IsActive)
        {
            return null;
        }

        var requesterMembers = await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId
                && m.UserId == request.RequestUserId
                && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        if (!requesterMembers.Any())
        {
            throw new SddpException("REOPEN_FAILED", "Only DM members can reopen direct messages");
        }

        var reopenResult = dm.Reopen();
        reopenResult.EnsureSuccess("REOPEN_FAILED");
        await (dmRepo.UpdateAsync(dm, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var memberCount = (await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId && m.IsActive,
            cancellationToken)).ConfigureAwait(false)).Count;

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "reopen",
            ResourceType: "direct_message",
            ResourceId: request.ConversationId,
            Payload: new { dm.Name },
            TenantId: request.TenantId,
            ProjectId: null);

        return ConversationMapping.MapConversationSummaryDto(dm, memberCount, 0, null);
    }
}

// =============================================================================
// ArchiveDirectMessageCommand — Concluded → Archived
// =============================================================================

/// <summary>
/// DM (Concluded → Archived)
/// </summary>
public sealed record ArchiveDirectMessageCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId RequestUserId) : ICommand<ConversationSummaryDto?>, IAuditableRequest<ConversationSummaryDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ConversationSummaryDto? response) => AuditLog;
}

public sealed class ArchiveDirectMessageCommandHandler : IRequestHandler<ArchiveDirectMessageCommand, ConversationSummaryDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public ArchiveDirectMessageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConversationSummaryDto?> Handle(ArchiveDirectMessageCommand request, CancellationToken cancellationToken)
    {
        var dmRepo = _unitOfWork.Repository<DirectMessage>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();

        var dm = await (dmRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (dm is null || dm.TenantId != request.TenantId || !dm.IsActive)
        {
            return null;
        }

        var requesterMembers = await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId
                && m.UserId == request.RequestUserId
                && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        if (!requesterMembers.Any())
        {
            throw new SddpException("ARCHIVE_FAILED", "Only DM members can archive direct messages");
        }

        var archiveResult = dm.Archive();
        archiveResult.EnsureSuccess("ARCHIVE_FAILED");
        await (dmRepo.UpdateAsync(dm, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var memberCount = (await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId && m.IsActive,
            cancellationToken)).ConfigureAwait(false)).Count;

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "archive",
            ResourceType: "direct_message",
            ResourceId: request.ConversationId,
            Payload: new { dm.Name },
            TenantId: request.TenantId,
            ProjectId: null);

        return ConversationMapping.MapConversationSummaryDto(dm, memberCount, 0, null);
    }
}
