using Sddp.Abstractions.Enums;

namespace Sddp.Api.Controllers;

/// <summary>
/// message create DTO
/// </summary>
public record CreateMessageDto(
    MessageType Type,
    string Content,
    string[]? References = null,
    string? ReplyToId = null);

/// <summary>
/// Topic create DTO
/// </summary>
public record CreateTopicRequestDto(
    string Title,
    string? InitialMessageContent = null);

/// <summary>
/// Topic update DTO
/// </summary>
public record UpdateTopicRequestDto(
    string Title);

/// <summary>
/// Topic DTO
/// </summary>
public record CloseTopicRequestDto(
    string? DecisionSpecId = null);

/// <summary>
/// Conversation create DTO
/// </summary>
public record CreateConversationRequestDto(
    string Name,
    ConversationType ConversationType = ConversationType.Channel,
    ConversationVisibility Visibility = ConversationVisibility.Public,
    ConversationScope? Scope = null,
    string? Description = null,
    int SortOrder = 0);

/// <summary>
/// DTO
/// </summary>
public record AddMembersDto(string[] UserIds);

/// <summary>
/// channel Conclude DTO
/// </summary>
public record ConcludeChannelRequestDto(
    string? DecisionSpecId = null);

/// <summary>
/// message DTO
/// </summary>
public record EditMessageRequestDto(string Content);
