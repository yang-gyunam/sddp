using Sddp.Abstractions.Enums;

namespace Sddp.Abstractions.DTOs;

// ============================================
// Messages
// ============================================

public record MessageDto(
    string Id,
    string ConversationId,
    UserRefDto Sender,
    MessageType Type,
    string Content,
    string[] References,
    string? ReplyToId,
    bool IsEdited,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record ParticipantDto(
    string Id,
    UserRefDto User,
    ParticipantType Type,
    string? Role,
    DateTimeOffset JoinedAt,
    DateTimeOffset? LeftAt,
    bool IsActive);

public record MessagesPageDto(
    IEnumerable<MessageDto> Messages,
    string? NextCursor,
    bool HasMore);

public record UnreadCountsDto(
    int TotalUnread,
    IEnumerable<ConversationUnreadDto> ByConversation);

public record ConversationUnreadDto(
    string ConversationId,
    string Topic,
    int UnreadCount,
    DateTimeOffset? LastMessageAt);

public record UserConversationSettingsDto(
    string ConversationId,
    bool IsStarred,
    bool IsMuted,
    DateTimeOffset? LastReadAt);

public record DMDto(
    string Id,
    UserRefDto OtherUser,
    int UnreadCount,
    MessageDto? LastMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    ChannelStatus? Status = null);

// ============================================
// Conversations
// ============================================

public record ConversationSummaryDto(
    string Id,
    string? ProjectId,
    string Name,
    string? Description,
    ConversationType ConversationType,
    ConversationVisibility Visibility,
    ConversationScope Scope,
    bool IsArchived,
    int SortOrder,
    int MemberCount,
    int UnreadCount,
    DateTimeOffset? LastMessageAt,
    ChannelStatus? ChannelStatus = null,
    string? DecisionSpecId = null);

public record ConversationSearchResultDto(
    string Id,
    string Name,
    string? Description,
    ConversationType ConversationType);

public record ConversationMessageDto(
    string Id,
    string ConversationId,
    UserRefDto Sender,
    MessageType Type,
    string Content,
    string[] References,
    string? ReplyToId,
    bool IsEdited,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record ConversationMessagesPageDto(
    IEnumerable<ConversationMessageDto> Messages,
    string? NextCursor,
    bool HasMore);

// ============================================
// Topics
// ============================================

public record TopicDto(
    string Id,
    string ForumId,
    string Title,
    UserRefDto Author,
    TopicStatus Status,
    bool IsPinned,
    bool IsLocked,
    string? DecisionSpecId,
    int MessageCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record TopicDetailDto(
    string Id,
    string ForumId,
    string ForumName,
    string Title,
    UserRefDto Author,
    TopicStatus Status,
    bool IsPinned,
    bool IsLocked,
    string? DecisionSpecId,
    IEnumerable<ForumMessageDto> RecentMessages,
    int MessageCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record TopicsPageDto(
    IEnumerable<TopicDto> Topics,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);

public record ForumMessageDto(
    string Id,
    string TopicId,
    UserRefDto Sender,
    MessageType Type,
    string Content,
    string[] References,
    string? ReplyToId,
    bool IsEdited,
    bool IsPinned,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record ForumMessagesPageDto(
    IEnumerable<ForumMessageDto> Messages,
    string? NextCursor,
    bool HasMore);

// ============================================
// Post Message Result (includes auto-participation flag)
// ============================================

public record PostMessageResultDto(
    ConversationMessageDto Message,
    bool IsNewParticipant,
    string? NewParticipantUserId,
    string? NewParticipantDisplayName);
