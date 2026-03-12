using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// Forum entity backed by the forums table (TPT).
/// Database table: forums (PK = FK -> conversations.id).
/// </summary>
public class Forum : Conversation
{
    /// <summary>
    /// Topics in the forum.
    /// </summary>
    public ICollection<Topic> Topics { get; private set; } = [];

    // Parameterless constructor for EF Core.
    private Forum() { }

    public Forum(
        GlobalUniqueId tenantId,
        GlobalUniqueId? projectId,
        string name,
        ConversationVisibility visibility = ConversationVisibility.Public,
        int sortOrder = 0,
        ConversationScope? scope = null)
        : base(tenantId, projectId, name, ConversationType.Forum, visibility, sortOrder, scope)
    {
    }

    /// <summary>
    /// Indicates whether a topic can be added to the forum.
    /// </summary>
    public bool CanAddTopic() => !IsArchived && IsActive;
}
