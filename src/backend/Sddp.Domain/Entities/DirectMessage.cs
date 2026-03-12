using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Result = Sddp.Abstractions.Base.Result;

namespace Sddp.Domain.Entities;

/// <summary>
/// DirectMessage entity - 1:1 personal conversation (TPT)
/// DB: direct_messages (PK = FK → conversations.id)
/// status: Active → Concluded → Archived
/// </summary>
public class DirectMessage : Conversation
{
    /// <summary>DirectMessage status</summary>
    public DirectMessageStatus Status { get; private set; } = DirectMessageStatus.Active;

    /// <summary> Spec ID (Concluded settings)</summary>
    public GlobalUniqueId? DecisionSpecId { get; private set; }

    // EF Core default create
    private DirectMessage() { }

    public DirectMessage(
        GlobalUniqueId tenantId,
        GlobalUniqueId? projectId,
        string name,
        ConversationScope? scope = null)
        : base(tenantId, projectId, name, ConversationType.DirectMessage, ConversationVisibility.Private, 0, scope)
    {
        Status = DirectMessageStatus.Active;
    }

    /// <summary>
    /// DM (Active → Concluded)
    /// </summary>
    public Result Conclude(GlobalUniqueId? decisionSpecId = null)
    {
        if (!Status.CanConclude())
            return DomainError.InvalidTransition("Conclude", Status.ToString());

        Status = DirectMessageStatus.Concluded;
        DecisionSpecId = decisionSpecId;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// DM (Concluded → Active)
    /// </summary>
    public Result Reopen()
    {
        if (!Status.CanReopen())
            return DomainError.InvalidTransition("Reopen", Status.ToString());

        Status = DirectMessageStatus.Active;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// DM (Concluded → Archived)
    /// </summary>
    public new Result Archive()
    {
        if (!Status.CanArchive())
            return DomainError.InvalidTransition("Archive", Status.ToString());

        Status = DirectMessageStatus.Archived;
        base.Archive();
        return Result.Success();
    }

    /// <summary>
    /// message (Active status)
    /// </summary>
    public new bool CanAddMessage() => Status.CanAddMessage() && IsActive;
}
