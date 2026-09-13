using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Knowledge;

/// <summary>
/// Resolves whether a mention/backlink target is visible to a specific user.
///
/// Project scope alone is not sufficient for every target type: a Decision is a conversation
/// message, and a private conversation is only readable by its active members. Knowledge must
/// reuse that rule rather than re-deriving it, otherwise a Page can surface the existence and
/// title of a Decision from a conversation the reader was never part of.
/// </summary>
internal static class KnowledgeTargetVisibility
{
    public static async Task<bool> IsTargetVisibleAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId userId,
        KnowledgeTargetType targetType,
        GlobalUniqueId targetId,
        CancellationToken cancellationToken)
    {
        return targetType switch
        {
            KnowledgeTargetType.Spec => await ExistsAsync<Spec>(
                unitOfWork,
                entity => entity.Id == targetId
                    && entity.TenantId == tenantId
                    && entity.ProjectId == projectId
                    && entity.IsActive,
                cancellationToken),
            KnowledgeTargetType.Requirement => await ExistsAsync<Requirement>(
                unitOfWork,
                entity => entity.Id == targetId
                    && entity.TenantId == tenantId
                    && entity.ProjectId == projectId
                    && entity.IsActive,
                cancellationToken),
            KnowledgeTargetType.Artifact => await ExistsAsync<ArtifactTracking>(
                unitOfWork,
                entity => entity.Id == targetId
                    && entity.TenantId == tenantId
                    && entity.ProjectId == projectId
                    && entity.IsActive,
                cancellationToken),
            KnowledgeTargetType.Decision => await IsDecisionVisibleAsync(
                unitOfWork,
                tenantId,
                projectId,
                userId,
                targetId,
                cancellationToken),
            _ => false
        };
    }

    private static async Task<bool> IsDecisionVisibleAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId userId,
        GlobalUniqueId targetId,
        CancellationToken cancellationToken)
    {
        var messages = await unitOfWork.Repository<Message>().FindAsync(
            entity => entity.Id == targetId
                && entity.TenantId == tenantId
                && entity.ProjectId == projectId
                && entity.Type == MessageType.Decision
                && entity.IsActive,
            cancellationToken);
        var message = messages.FirstOrDefault();
        if (message is null)
        {
            return false;
        }

        // A Decision that is not attached to a conversation carries no conversation ACL, so the
        // project scope already checked above is the whole contract.
        if (!message.ConversationId.HasValue)
        {
            return true;
        }

        var conversationId = message.ConversationId.Value;
        var conversations = await unitOfWork.Repository<Conversation>().FindAsync(
            entity => entity.Id == conversationId
                && entity.TenantId == tenantId
                && entity.IsActive,
            cancellationToken);
        var conversation = conversations.FirstOrDefault();
        if (conversation is null)
        {
            return false;
        }

        if (conversation.Visibility != ConversationVisibility.Private)
        {
            return true;
        }

        // Mirrors GetConversationByIdQueryHandler: a private conversation requires active membership.
        var memberCount = await unitOfWork.Repository<ConversationMember>().CountAsync(
            member => member.ConversationId == conversationId
                && member.UserId == userId
                && member.IsActive,
            cancellationToken);
        return memberCount > 0;
    }

    private static async Task<bool> ExistsAsync<TEntity>(
        IUnitOfWork unitOfWork,
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken)
        where TEntity : Sddp.Abstractions.Base.EntityBase
    {
        var entities = await unitOfWork.Repository<TEntity>().FindAsync(predicate, cancellationToken);
        return entities.Count > 0;
    }
}
