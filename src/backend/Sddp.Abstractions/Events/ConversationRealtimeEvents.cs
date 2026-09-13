namespace Sddp.Abstractions.Events;

public sealed record ConversationMemberRemovedEvent(
    string ConversationId,
    string RemovedUserId,
    string RemovedUserName);

/// <inheritdoc cref="ConversationMemberRemovedEvent"/>
public sealed record ProjectChannelCreatedEvent(
    IReadOnlyList<string> MemberUserIds,
    string ConversationId,
    string ProjectId,
    string ChannelName,
    string CreatedByUserId);

/// <inheritdoc cref="ConversationMemberRemovedEvent"/>
public sealed record ConversationClosedEvent(
    string ConversationId,
    string? DecisionSpecId,
    string? ConcludedByUserName);

/// <inheritdoc cref="ConversationMemberRemovedEvent"/>
public sealed record DirectMessageConcludedEvent(
    IReadOnlyList<string> MemberUserIds,
    string ConversationId,
    string ConcludedByUserName);
