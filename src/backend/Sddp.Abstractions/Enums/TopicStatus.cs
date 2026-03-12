namespace Sddp.Abstractions.Enums;

/// <summary>
/// Topic status (for topics table)
/// DB codes: TOPIC_STATUS/OPEN, CLOSED, ARCHIVED
/// </summary>
public enum TopicStatus
{
    /// <summary>Open - active topic</summary>
    Open = 0,

    /// <summary>Closed - terminated topic</summary>
    Closed = 1,

    /// <summary>Archived - archived topic</summary>
    Archived = 2
}

/// <summary>
/// TopicStatus extension methods
/// </summary>
public static class TopicStatusExtensions
{
    /// <summary>
    /// Checks whether messages can be added in the current status
    /// </summary>
    public static bool CanAddMessage(this TopicStatus status)
    {
        return status == TopicStatus.Open;
    }

    /// <summary>
    /// Checks whether the topic can be closed in the current status
    /// </summary>
    public static bool CanClose(this TopicStatus status)
    {
        return status == TopicStatus.Open;
    }

    /// <summary>
    /// Checks whether the topic can be reopened in the current status
    /// </summary>
    public static bool CanReopen(this TopicStatus status)
    {
        return status == TopicStatus.Closed;
    }
}
