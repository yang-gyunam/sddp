using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Result = Sddp.Abstractions.Base.Result;

namespace Sddp.Domain.Entities;

/// <summary>
/// Channel entity - + (TPT)
/// DB: channels (PK = FK → conversations.id)
/// status: Active → Concluded → Archived
/// </summary>
public class Channel : Conversation
{
    /// <summary>Channel status</summary>
    public ChannelStatus Status { get; private set; } = ChannelStatus.Active;

    /// <summary> Spec ID (Concluded settings)</summary>
    public GlobalUniqueId? DecisionSpecId { get; private set; }

    // EF Core default create
    private Channel() { }

    public Channel(
        GlobalUniqueId tenantId,
        GlobalUniqueId? projectId,
        string name,
        ConversationVisibility visibility = ConversationVisibility.Public,
        int sortOrder = 0,
        ConversationScope? scope = null)
        : base(tenantId, projectId, name, ConversationType.Channel, visibility, sortOrder, scope)
    {
        Status = ChannelStatus.Active;
    }

    /// <summary>
    /// Private Channel create
    /// </summary>
    public static Channel CreatePrivate(
        GlobalUniqueId tenantId,
        GlobalUniqueId? projectId,
        string name)
    {
        return new Channel(
            tenantId,
            projectId,
            name,
            ConversationVisibility.Private);
    }

    /// <summary>
    /// channel (Active → Concluded)
    /// </summary>
    public Result Conclude(GlobalUniqueId? decisionSpecId = null)
    {
        if (!Status.CanConclude())
            return DomainError.InvalidTransition("Conclude", Status.ToString());

        Status = ChannelStatus.Concluded;
        DecisionSpecId = decisionSpecId;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// channel (Concluded → Active)
    /// </summary>
    public Result Reopen()
    {
        if (!Status.CanReopen())
            return DomainError.InvalidTransition("Reopen", Status.ToString());

        Status = ChannelStatus.Active;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// channel (Concluded → Archived)
    /// </summary>
    public new Result Archive()
    {
        if (!Status.CanArchive())
            return DomainError.InvalidTransition("Archive", Status.ToString());

        Status = ChannelStatus.Archived;
        base.Archive();
        return Result.Success();
    }

    /// <summary>
    /// message (Active status)
    /// </summary>
    public new bool CanAddMessage() => Status.CanAddMessage() && IsActive;
}
