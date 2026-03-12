using MediatR;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Conversations.Queries;

/// <summary>
/// user DTO
/// </summary>
public record InvitableUserDto(string Id, string DisplayName, string Email, bool IsAi);

/// <summary>
/// conversation user get
/// Owner/Moderator get
/// tenant user user
/// </summary>
public sealed record GetInvitableUsersQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId RequestUserId,
    string? Search) : IQuery<IReadOnlyList<InvitableUserDto>>, IAuditableRequest<IReadOnlyList<InvitableUserDto>>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(IReadOnlyList<InvitableUserDto> response) => AuditLog;
}

public sealed class GetInvitableUsersQueryHandler : IRequestHandler<GetInvitableUsersQuery, IReadOnlyList<InvitableUserDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetInvitableUsersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<InvitableUserDto>> Handle(GetInvitableUsersQuery request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
        var userRoleRepo = _unitOfWork.Repository<UserRole>();
        var userRepo = _unitOfWork.Repository<User>();

        // 1. Conversation + tenant
        var conversation = await (conversationRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (conversation is null
            || conversation.TenantId != request.TenantId
            || !conversation.IsActive)
        {
            throw new SddpException("GET_INVITABLE_USERS_FAILED", "Conversation not found");
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
            throw new SddpException("GET_INVITABLE_USERS_FAILED", "Only owners and moderators can view invitable users");
        }

        // 3. conversation user
        var existingMembers = await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var existingUserIds = existingMembers.Select(m => m.UserId).ToHashSet();

        // 4. user get
        HashSet<GlobalUniqueId> candidateUserIds;

        if (conversation.ProjectId.HasValue)
        {
            // ProjectScoped: project
            var projectMemberRepo = _unitOfWork.Repository<ProjectMember>();
            var projectMembers = await (projectMemberRepo.FindAsync(
                pm => pm.ProjectId == conversation.ProjectId.Value && pm.IsActive,
                cancellationToken)).ConfigureAwait(false);
            candidateUserIds = projectMembers
                .Select(pm => pm.UserId)
                .Distinct()
                .ToHashSet();
        }
        else
        {
            // TenantWide: tenant user all
            var tenantUserRoles = await (userRoleRepo.FindAsync(
                ur => ur.TenantId == request.TenantId && ur.IsActive,
                cancellationToken)).ConfigureAwait(false);
            candidateUserIds = tenantUserRoles
                .Select(ur => ur.UserId)
                .Distinct()
                .ToHashSet();
        }

        // 5. user user
        var allUsers = await (userRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var invitableUsers = allUsers
            .Where(u => u.IsActive
                && candidateUserIds.Contains(u.Id)
                && !existingUserIds.Contains(u.Id));

        // 6. search
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            invitableUsers = invitableUsers.Where(u =>
                u.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || u.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        // 7. audit log
        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "query_invitable_users",
            ResourceType: "conversation",
            ResourceId: request.ConversationId,
            Payload: new { Search = request.Search },
            TenantId: request.TenantId,
            ProjectId: null);

        // 8. 20
        return invitableUsers
            .OrderBy(u => u.DisplayName)
            .Take(20)
            .Select(u => new InvitableUserDto(
                u.Id.ToString(),
                u.DisplayName,
                u.Email,
                u.IsAI))
            .ToList();
    }
}
