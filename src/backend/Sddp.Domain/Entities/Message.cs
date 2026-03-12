using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// Message entity
/// Message for a Conversation or Topic (REQ-03 Conversation System)
/// DB table: messages (conversation_id FK -> conversations.id, topic_id FK -> topics.id)
/// </summary>
public class Message : EntityBase
{
    /// <summary>
    /// Tenant ID (for multi-tenancy)
    /// </summary>
    public GlobalUniqueId TenantId { get; private set; }

    /// <summary>
    /// Project ID (NULL = tenant-wide conversation message)
    /// </summary>
    public GlobalUniqueId? ProjectId { get; private set; }

    /// <summary>
    /// Conversation ID (FK) - direct Channel/DirectMessage message
    /// </summary>
    public GlobalUniqueId? ConversationId { get; private set; }

    /// <summary>
    /// Topic ID (FK) - Forum topic message
    /// </summary>
    public GlobalUniqueId? TopicId { get; private set; }

    /// <summary>
    /// Sender ID (FK)
    /// </summary>
    public GlobalUniqueId SenderId { get; private set; }

    /// <summary>
    /// Message type (REQ-03.3)
    /// </summary>
    public MessageType Type { get; private set; }

    /// <summary>
    /// Message content (Markdown supported)
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Referenced Spec/Requirement IDs
    /// </summary>
    public string[] References { get; private set; } = [];

    /// <summary>
    /// Reply target message ID (optional)
    /// </summary>
    public GlobalUniqueId? ReplyToId { get; private set; }

    /// <summary>
    /// Whether the message has been edited
    /// </summary>
    public bool IsEdited { get; private set; }

    /// <summary>
    /// Edit timestamp
    /// </summary>
    public Timestamp? EditedAt { get; private set; }

    /// <summary>
    /// Whether the message is pinned
    /// </summary>
    public bool IsPinned { get; private set; }

    /// <summary>
    /// Conversation navigation property
    /// </summary>
    public Conversation? Conversation { get; private set; }

    /// <summary>
    /// Topic navigation property
    /// </summary>
    public Topic? Topic { get; private set; }

    /// <summary>
    /// Sender navigation property
    /// </summary>
    public User Sender { get; private set; } = null!;

    /// <summary>
    /// Reply-target navigation property
    /// </summary>
    public Message? ReplyTo { get; private set; }

    // Default constructor for EF Core
    private Message() { }

    /// <summary>
    /// Creates a Channel/DirectMessage message
    /// </summary>
    public static Message CreateForChannel(
        GlobalUniqueId tenantId,
        GlobalUniqueId? projectId,
        GlobalUniqueId conversationId,
        GlobalUniqueId senderId,
        MessageType type,
        string content,
        string[]? references = null,
        GlobalUniqueId? replyToId = null)
    {
        return new Message
        {
            TenantId = tenantId,
            ProjectId = projectId,
            ConversationId = conversationId,
            SenderId = senderId,
            Type = type,
            Content = content,
            References = references ?? [],
            ReplyToId = replyToId,
            IsEdited = false,
            IsPinned = false
        };
    }

    /// <summary>
    /// Creates a Topic message
    /// </summary>
    public static Message CreateForTopic(
        GlobalUniqueId tenantId,
        GlobalUniqueId? projectId,
        GlobalUniqueId topicId,
        GlobalUniqueId senderId,
        MessageType type,
        string content,
        string[]? references = null,
        GlobalUniqueId? replyToId = null)
    {
        return new Message
        {
            TenantId = tenantId,
            ProjectId = projectId,
            TopicId = topicId,
            SenderId = senderId,
            Type = type,
            Content = content,
            References = references ?? [],
            ReplyToId = replyToId,
            IsEdited = false,
            IsPinned = false
        };
    }

    /// <summary>
    /// Edits the message content
    /// </summary>
    public void Edit(string content)
    {
        Content = content;
        IsEdited = true;
        EditedAt = Timestamp.Now;
        MarkAsModified();
    }

    /// <summary>
    /// Adds a reference
    /// </summary>
    public void AddReference(string referenceId)
    {
        var refs = References.ToList();
        if (!refs.Contains(referenceId))
        {
            refs.Add(referenceId);
            References = refs.ToArray();
            MarkAsModified();
        }
    }

    /// <summary>
    /// Pins the message
    /// </summary>
    public void Pin()
    {
        IsPinned = true;
        MarkAsModified();
    }

    /// <summary>
    /// Unpins the message
    /// </summary>
    public void Unpin()
    {
        IsPinned = false;
        MarkAsModified();
    }

    /// <summary>
    /// Returns whether this is an AI-generated message
    /// </summary>
    public bool IsAiGenerated() => Type.IsAiGenerated();

    /// <summary>
    /// Returns whether this is a direct conversation message
    /// </summary>
    public bool IsConversationMessage() => ConversationId.HasValue;

    /// <summary>
    /// Returns whether this is a topic message
    /// </summary>
    public bool IsTopicMessage() => TopicId.HasValue;
}
