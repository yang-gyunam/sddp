using Sddp.Abstractions.Enums;

namespace Sddp.Abstractions.Constants;

/// <summary>
/// codes Well-known UUID.
/// UUID: 00000000-0000-0000-0006-GGGGGGSSSSSS
/// (0006 = codes namespace, GGGGGG = group number, SSSSSS = sort_order)
/// </summary>
public static class WellKnownCodes
{
    // -------------------------------------------------------------------------
    // SpecStatus (Group 01)
    // -------------------------------------------------------------------------
    public static readonly Dictionary<SpecStatus, Guid> SpecStatusToId =
        new Dictionary<SpecStatus, Guid>
        {
            [SpecStatus.Draft] = Guid.Parse("00000000-0000-0000-0006-000001000001"),
            [SpecStatus.InReview] = Guid.Parse("00000000-0000-0000-0006-000001000002"),
            [SpecStatus.Approved] = Guid.Parse("00000000-0000-0000-0006-000001000003"),
            [SpecStatus.Locked] = Guid.Parse("00000000-0000-0000-0006-000001000004"),
        };

    public static readonly Dictionary<Guid, SpecStatus> IdToSpecStatus =
        SpecStatusToId.ToDictionary(kv => kv.Value, kv => kv.Key);

    // -------------------------------------------------------------------------
    // SignOffDecision (Group 15)
    // -------------------------------------------------------------------------
    public static readonly Dictionary<SignOffDecision, Guid> SignOffDecisionToId =
        new Dictionary<SignOffDecision, Guid>
        {
            [SignOffDecision.Pending] = Guid.Parse("00000000-0000-0000-0006-000015000001"),
            [SignOffDecision.Approved] = Guid.Parse("00000000-0000-0000-0006-000015000002"),
            [SignOffDecision.Rejected] = Guid.Parse("00000000-0000-0000-0006-000015000003"),
            [SignOffDecision.Conditional] = Guid.Parse("00000000-0000-0000-0006-000015000004"),
        };

    public static readonly Dictionary<Guid, SignOffDecision> IdToSignOffDecision =
        SignOffDecisionToId.ToDictionary(kv => kv.Value, kv => kv.Key);

    // -------------------------------------------------------------------------
    // RequirementLevel (Group 04)
    // -------------------------------------------------------------------------
    public static readonly Dictionary<RequirementLevel, Guid> RequirementLevelToId =
        new Dictionary<RequirementLevel, Guid>
        {
            [RequirementLevel.A] = Guid.Parse("00000000-0000-0000-0006-000004000001"),
            [RequirementLevel.B] = Guid.Parse("00000000-0000-0000-0006-000004000002"),
            [RequirementLevel.C] = Guid.Parse("00000000-0000-0000-0006-000004000003"),
        };

    public static readonly Dictionary<Guid, RequirementLevel> IdToRequirementLevel =
        RequirementLevelToId.ToDictionary(kv => kv.Value, kv => kv.Key);

    // -------------------------------------------------------------------------
    // RequirementStatus (Group 03)
    // -------------------------------------------------------------------------
    public static readonly Dictionary<RequirementStatus, Guid> RequirementStatusToId =
        new Dictionary<RequirementStatus, Guid>
        {
            [RequirementStatus.Draft] = Guid.Parse("00000000-0000-0000-0006-000003000001"),
            [RequirementStatus.InReview] = Guid.Parse("00000000-0000-0000-0006-000003000002"),
            [RequirementStatus.Approved] = Guid.Parse("00000000-0000-0000-0006-000003000003"),
            [RequirementStatus.Deprecated] = Guid.Parse("00000000-0000-0000-0006-000003000004"),
        };

    public static readonly Dictionary<Guid, RequirementStatus> IdToRequirementStatus =
        RequirementStatusToId.ToDictionary(kv => kv.Value, kv => kv.Key);

    // -------------------------------------------------------------------------
    // MessageType (Group 07)
    // -------------------------------------------------------------------------
    public static readonly Dictionary<MessageType, Guid> MessageTypeToId =
        new Dictionary<MessageType, Guid>
        {
            [MessageType.Proposal] = Guid.Parse("00000000-0000-0000-0006-000007000001"),
            [MessageType.Question] = Guid.Parse("00000000-0000-0000-0006-000007000002"),
            [MessageType.Objection] = Guid.Parse("00000000-0000-0000-0006-000007000003"),
            [MessageType.Reference] = Guid.Parse("00000000-0000-0000-0006-000007000004"),
            [MessageType.Decision] = Guid.Parse("00000000-0000-0000-0006-000007000005"),
            [MessageType.AiReminder] = Guid.Parse("00000000-0000-0000-0006-000007000006"),
            [MessageType.AiSummary] = Guid.Parse("00000000-0000-0000-0006-000007000007"),
            [MessageType.AiSuggestion] = Guid.Parse("00000000-0000-0000-0006-000007000008"),
            [MessageType.Normal] = Guid.Parse("00000000-0000-0000-0006-000007000009"),
        };

    public static readonly Dictionary<Guid, MessageType> IdToMessageType =
        MessageTypeToId.ToDictionary(kv => kv.Value, kv => kv.Key);

    // -------------------------------------------------------------------------
    // TopicStatus (Group 20) - topics
    // -------------------------------------------------------------------------
    public static readonly Dictionary<TopicStatus, Guid> TopicStatusToId =
        new Dictionary<TopicStatus, Guid>
        {
            [TopicStatus.Open] = Guid.Parse("00000000-0000-0000-0006-000020000001"),
            [TopicStatus.Closed] = Guid.Parse("00000000-0000-0000-0006-000020000002"),
            [TopicStatus.Archived] = Guid.Parse("00000000-0000-0000-0006-000020000003"),
        };

    public static readonly Dictionary<Guid, TopicStatus> IdToTopicStatus =
        TopicStatusToId.ToDictionary(kv => kv.Value, kv => kv.Key);

    // -------------------------------------------------------------------------
    // ChannelStatus (Group 21) - channels
    // -------------------------------------------------------------------------
    public static readonly Dictionary<ChannelStatus, Guid> ChannelStatusToId =
        new Dictionary<ChannelStatus, Guid>
        {
            [ChannelStatus.Active] = Guid.Parse("00000000-0000-0000-0006-000021000001"),
            [ChannelStatus.Concluded] = Guid.Parse("00000000-0000-0000-0006-000021000002"),
            [ChannelStatus.Archived] = Guid.Parse("00000000-0000-0000-0006-000021000003"),
        };

    public static readonly Dictionary<Guid, ChannelStatus> IdToChannelStatus =
        ChannelStatusToId.ToDictionary(kv => kv.Value, kv => kv.Key);

    // -------------------------------------------------------------------------
    // TermCategory (Group 10)
    // -------------------------------------------------------------------------
    public static readonly Dictionary<TermCategory, Guid> TermCategoryToId =
        new Dictionary<TermCategory, Guid>
        {
            [TermCategory.Technical] = Guid.Parse("00000000-0000-0000-0006-000010000001"),
            [TermCategory.Business] = Guid.Parse("00000000-0000-0000-0006-000010000002"),
            [TermCategory.Abbreviation] = Guid.Parse("00000000-0000-0000-0006-000010000003"),
            [TermCategory.Domain] = Guid.Parse("00000000-0000-0000-0006-000010000004"),
            [TermCategory.Architecture] = Guid.Parse("00000000-0000-0000-0006-000010000005"),
            [TermCategory.Infrastructure] = Guid.Parse("00000000-0000-0000-0006-000010000006"),
            [TermCategory.Security] = Guid.Parse("00000000-0000-0000-0006-000010000007"),
            [TermCategory.Compliance] = Guid.Parse("00000000-0000-0000-0006-000010000008"),
            [TermCategory.DesignPattern] = Guid.Parse("00000000-0000-0000-0006-000010000009"),
        };

    public static readonly Dictionary<Guid, TermCategory> IdToTermCategory =
        TermCategoryToId.ToDictionary(kv => kv.Value, kv => kv.Key);

    // -------------------------------------------------------------------------
    // GlossaryTermStatus (Group 09)
    // -------------------------------------------------------------------------
    public static readonly Dictionary<GlossaryTermStatus, Guid> GlossaryTermStatusToId =
        new Dictionary<GlossaryTermStatus, Guid>
        {
            [GlossaryTermStatus.Draft] = Guid.Parse("00000000-0000-0000-0006-000009000001"),
            [GlossaryTermStatus.Active] = Guid.Parse("00000000-0000-0000-0006-000009000002"),
            [GlossaryTermStatus.Deprecated] = Guid.Parse("00000000-0000-0000-0006-000009000003"),
        };

    public static readonly Dictionary<Guid, GlossaryTermStatus> IdToGlossaryTermStatus =
        GlossaryTermStatusToId.ToDictionary(kv => kv.Value, kv => kv.Key);

    // -------------------------------------------------------------------------
    // RelationType (Group 14)
    // -------------------------------------------------------------------------
    public static readonly Dictionary<RelationType, Guid> RelationTypeToId =
        new Dictionary<RelationType, Guid>
        {
            [RelationType.Supersedes] = Guid.Parse("00000000-0000-0000-0006-000014000001"),
            [RelationType.EvolvesFrom] = Guid.Parse("00000000-0000-0000-0006-000014000002"),
            [RelationType.Extends] = Guid.Parse("00000000-0000-0000-0006-000014000003"),
            [RelationType.ConflictsWith] = Guid.Parse("00000000-0000-0000-0006-000014000004"),
            [RelationType.DependsOn] = Guid.Parse("00000000-0000-0000-0006-000014000005"),
            [RelationType.Implements] = Guid.Parse("00000000-0000-0000-0006-000014000006"),
            [RelationType.Replaces] = Guid.Parse("00000000-0000-0000-0006-000014000007"),
            [RelationType.Affects] = Guid.Parse("00000000-0000-0000-0006-000014000008"),
        };

    public static readonly Dictionary<Guid, RelationType> IdToRelationType =
        RelationTypeToId.ToDictionary(kv => kv.Value, kv => kv.Key);

    // -------------------------------------------------------------------------
    // TaskItemStatus (Group 11)
    // -------------------------------------------------------------------------
    public static readonly Dictionary<TaskItemStatus, Guid> TaskItemStatusToId =
        new Dictionary<TaskItemStatus, Guid>
        {
            [TaskItemStatus.ToDo] = Guid.Parse("00000000-0000-0000-0006-000011000001"),
            [TaskItemStatus.InProgress] = Guid.Parse("00000000-0000-0000-0006-000011000002"),
            [TaskItemStatus.Done] = Guid.Parse("00000000-0000-0000-0006-000011000003"),
            [TaskItemStatus.Blocked] = Guid.Parse("00000000-0000-0000-0006-000011000004"),
            [TaskItemStatus.Backlog] = Guid.Parse("00000000-0000-0000-0006-000011000005"),
        };

    public static readonly Dictionary<Guid, TaskItemStatus> IdToTaskItemStatus =
        TaskItemStatusToId.ToDictionary(kv => kv.Value, kv => kv.Key);

    // -------------------------------------------------------------------------
    // TaskItemPriority (Group 12)
    // -------------------------------------------------------------------------
    public static readonly Dictionary<TaskItemPriority, Guid> TaskItemPriorityToId =
        new Dictionary<TaskItemPriority, Guid>
        {
            [TaskItemPriority.Low] = Guid.Parse("00000000-0000-0000-0006-000012000001"),
            [TaskItemPriority.Medium] = Guid.Parse("00000000-0000-0000-0006-000012000002"),
            [TaskItemPriority.High] = Guid.Parse("00000000-0000-0000-0006-000012000003"),
            [TaskItemPriority.Urgent] = Guid.Parse("00000000-0000-0000-0006-000012000004"),
        };

    public static readonly Dictionary<Guid, TaskItemPriority> IdToTaskItemPriority =
        TaskItemPriorityToId.ToDictionary(kv => kv.Value, kv => kv.Key);
}
