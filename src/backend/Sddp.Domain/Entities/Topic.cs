using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Result = Sddp.Abstractions.Base.Result;

namespace Sddp.Domain.Entities;

/// <summary>
/// Topic entity - a topic inside a forum
/// DB table: topics (forum_id FK -> forums.id)
/// </summary>
public class Topic : EntityBase
{
    /// <summary>
    /// Forum ID (FK → forums.id)
    /// </summary>
    public GlobalUniqueId ForumId { get; private set; }

    /// <summary>
    /// Topic title
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Author ID
    /// </summary>
    public GlobalUniqueId AuthorId { get; private set; }

    /// <summary>
    /// Topic status
    /// </summary>
    public TopicStatus Status { get; private set; } = TopicStatus.Open;

    /// <summary>
    /// Whether the topic is pinned
    /// </summary>
    public bool IsPinned { get; private set; }

    /// <summary>
    /// Whether the topic is locked (no replies allowed)
    /// </summary>
    public bool IsLocked { get; private set; }

    /// <summary>
    /// Linked decision spec ID (for decision topics)
    /// </summary>
    public GlobalUniqueId? DecisionSpecId { get; private set; }

    /// <summary>
    /// Forum navigation property
    /// </summary>
    public Forum? Forum { get; private set; }

    /// <summary>
    /// Author navigation property
    /// </summary>
    public User? Author { get; private set; }

    /// <summary>
    /// Messages
    /// </summary>
    public ICollection<Message> Messages { get; private set; } = [];

    // Default constructor for EF Core
    private Topic() { }

    /// <summary>
    /// Creates a new topic
    /// </summary>
    public Topic(
        GlobalUniqueId forumId,
        GlobalUniqueId authorId,
        string title)
    {
        ForumId = forumId;
        AuthorId = authorId;
        Title = title;
        Status = TopicStatus.Open;
        IsPinned = false;
        IsLocked = false;
    }

    /// <summary>
    /// Updates the title
    /// </summary>
    public Result UpdateTitle(string title)
    {
        if (Status != TopicStatus.Open)
            return DomainError.InvalidStatus("update title", Status.ToString());

        Title = title;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Closes the topic
    /// </summary>
    public Result Close(GlobalUniqueId? decisionSpecId = null)
    {
        if (!Status.CanClose())
            return DomainError.InvalidTransition("Close", Status.ToString());

        Status = TopicStatus.Closed;
        DecisionSpecId = decisionSpecId;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Reopens the topic
    /// </summary>
    public Result Reopen()
    {
        if (!Status.CanReopen())
            return DomainError.InvalidTransition("Reopen", Status.ToString());

        Status = TopicStatus.Open;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Archives the topic
    /// </summary>
    public Result Archive()
    {
        if (Status != TopicStatus.Closed)
            return DomainError.InvalidTransition("Archive", Status.ToString());

        Status = TopicStatus.Archived;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Toggles the pinned state
    /// </summary>
    public bool TogglePin()
    {
        IsPinned = !IsPinned;
        MarkAsModified();
        return IsPinned;
    }

    /// <summary>
    /// Toggles the locked state
    /// </summary>
    public bool ToggleLock()
    {
        IsLocked = !IsLocked;
        MarkAsModified();
        return IsLocked;
    }

    /// <summary>
    /// Returns whether messages can be added
    /// </summary>
    public bool CanAddMessage() => Status.CanAddMessage() && !IsLocked && IsActive;
}
