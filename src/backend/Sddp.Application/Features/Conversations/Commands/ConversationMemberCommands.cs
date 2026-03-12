using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Conversations.Commands;

/// <summary>
/// Conversation
/// </summary>
public sealed record AddConversationMembersCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId RequestUserId,
    IReadOnlyList<GlobalUniqueId> UserIds) : ICommand<IReadOnlyList<ParticipantDto>>, IAuditableRequest<IReadOnlyList<ParticipantDto>>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(IReadOnlyList<ParticipantDto> response) => AuditLog;
}

public sealed class AddConversationMembersCommandHandler : IRequestHandler<AddConversationMembersCommand, IReadOnlyList<ParticipantDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddConversationMembersCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ParticipantDto>> Handle(AddConversationMembersCommand request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
        var userRepo = _unitOfWork.Repository<User>();

        // 1. Conversation + tenant
        var conversation = await (conversationRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (conversation is null
            || conversation.TenantId != request.TenantId
            || !conversation.IsActive)
        {
            throw new SddpException("ADD_MEMBERS_FAILED", "Conversation not found");
        }

        // 2. Owner/Moderator
        var requesterMembers = await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId
                && m.UserId == request.RequestUserId
                && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var requester = requesterMembers.FirstOrDefault();
        if (requester is null || !requester.Role.CanManageMembers())
        {
            throw new SddpException("ADD_MEMBERS_FAILED", "Only owners and moderators can add members");
        }

        // 3. userId
        var existingMembers = await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var existingUserIds = existingMembers.Select(m => m.UserId).ToHashSet();

        // 4. new
        var newUserIds = request.UserIds
            .Where(uid => !existingUserIds.Contains(uid))
            .ToList();

        // Validate project member constraint for ProjectScoped conversations
        if (conversation.ProjectId.HasValue && newUserIds.Count > 0)
        {
            await (DirectMessageCommandHelper.EnsureProjectMembersAsync(
                _unitOfWork,
                conversation.ProjectId.Value,
                newUserIds,
                "ADD_MEMBERS_FAILED",
                "Only project members can be added to project-scoped conversations",
                cancellationToken)).ConfigureAwait(false);
        }

        if (newUserIds.Count > 0)
        {
            // User entity is_ai
            var users = await (userRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
            var userMap = users.ToDictionary(u => u.Id, u => u);

            foreach (var userId in newUserIds)
            {
                var isAi = userMap.TryGetValue(userId, out var user) && user.IsAI;
                var participantType = isAi ? ParticipantType.AI : ParticipantType.Human;

                var member = new ConversationMember(
                    request.ConversationId,
                    userId,
                    ConversationMemberRole.Member,
                    participantType);
                await (memberRepo.AddAsync(member, cancellationToken)).ConfigureAwait(false);
            }

            await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
        }

        // 5. all
        var allMembers = await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId && m.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var allUsers = await (userRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var allUserMap = allUsers.ToDictionary(u => u.Id, u => u);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "add_members",
            ResourceType: "conversation",
            ResourceId: request.ConversationId,
            Payload: new { AddedUserIds = newUserIds.Select(id => id.ToString()).ToList() },
            TenantId: request.TenantId,
            ProjectId: null);

        return allMembers
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.JoinedAt.ToDateTimeOffset())
            .Select(m =>
            {
                var user = allUserMap.TryGetValue(m.UserId, out var u) ? u : null;
                var userRef = new UserRefDto(m.UserId.ToString(), user?.DisplayName ?? "Unknown", user?.AvatarUrl);
                return ConversationMapping.MapParticipantDto(m, userRef);
            })
            .ToList();
    }
}

/// <summary>
/// Conversation (soft delete)
/// </summary>
public sealed record RemoveConversationMemberCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId RequestUserId,
    GlobalUniqueId TargetUserId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class RemoveConversationMemberCommandHandler : IRequestHandler<RemoveConversationMemberCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConversationHubService _hubService;

    public RemoveConversationMemberCommandHandler(IUnitOfWork unitOfWork, IConversationHubService hubService)
    {
        _unitOfWork = unitOfWork;
        _hubService = hubService;
    }

    public async Task<bool> Handle(RemoveConversationMemberCommand request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();

        var conversation = await (conversationRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (conversation is null
            || conversation.TenantId != request.TenantId
            || !conversation.IsActive)
        {
            return false;
        }

        var isSelfRemoval = request.RequestUserId == request.TargetUserId;

        // get
        var requesterMembers = await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId
                && m.UserId == request.RequestUserId
                && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var requester = requesterMembers.FirstOrDefault();

        // Self-removal(Leave), permission
        if (!isSelfRemoval && (requester is null || !requester.Role.CanManageMembers()))
        {
            throw new SddpException("REMOVE_MEMBER_FAILED", "Only owners and moderators can remove members");
        }

        // get
        var targetMembers = await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId
                && m.UserId == request.TargetUserId
                && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var target = targetMembers.FirstOrDefault();
        if (target is null)
        {
            throw new SddpException("REMOVE_MEMBER_FAILED", "Target user is not a member of this conversation");
        }

        // Owner (, Self-removal)
        if (!isSelfRemoval && target.Role == ConversationMemberRole.Owner)
        {
            throw new SddpException("REMOVE_MEMBER_FAILED", "Cannot remove the owner of a conversation");
        }

        // Moderator Owner
        if (!isSelfRemoval && target.Role == ConversationMemberRole.Moderator && requester!.Role != ConversationMemberRole.Owner)
        {
            throw new SddpException("REMOVE_MEMBER_FAILED", "Only the owner can remove moderators");
        }

        target.Deactivate();
        await (memberRepo.UpdateAsync(target, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // DM self-removal: conclude the DM (permanent closure)
        // DB status Concluded change — MemberRemoved
        // (UI participants, /)
        if (isSelfRemoval && conversation.ConversationType == ConversationType.DirectMessage)
        {
            var dmRepo = _unitOfWork.Repository<DirectMessage>();
            var dm = await (dmRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
            if (dm is not null && dm.Status == DirectMessageStatus.Active)
            {
                dm.Conclude();
                await (dmRepo.UpdateAsync(dm, cancellationToken)).ConfigureAwait(false);
                await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
            }
        }

        // Broadcast MemberRemoved event via SignalR
        var userRepo = _unitOfWork.Repository<User>();
        var removedUser = await (userRepo.GetByIdAsync(request.TargetUserId, cancellationToken)).ConfigureAwait(false);
        var removedUserName = removedUser?.DisplayName ?? "Unknown";
        await (_hubService.BroadcastMemberRemovedAsync(
            request.ConversationId.ToString(), request.TargetUserId.ToString(), removedUserName)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "remove_member",
            ResourceType: "conversation",
            ResourceId: request.ConversationId,
            Payload: new { TargetUserId = request.TargetUserId.ToString() },
            TenantId: request.TenantId,
            ProjectId: null);

        return true;
    }
}
