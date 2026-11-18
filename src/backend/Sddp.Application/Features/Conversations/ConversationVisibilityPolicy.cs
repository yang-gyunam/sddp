using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Conversations;

public static class ConversationVisibilityPolicy
{
    public static async Task<bool> CanAccessAsync(IUnitOfWork unitOfWork, Conversation conversation,
        GlobalUniqueId? userId, CancellationToken ct)
    {
        if (conversation.Visibility != ConversationVisibility.Private) return true;
        return userId is { } user && await unitOfWork.Repository<ConversationMember>().CountAsync(
            member => member.ConversationId == conversation.Id && member.UserId == user && member.IsActive, ct) > 0;
    }
}
